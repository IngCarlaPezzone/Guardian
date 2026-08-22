using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Guardian
{
    public sealed class Mission
    {
        public string Id { get; set; }
        public string Prompt { get; set; }
        public string CategoryId { get; set; }
        public string LevelId { get; set; }
        public string SkillId { get; set; }
        public string VariantId { get; set; }
        public List<string> AcceptedAnswers { get; set; }
    }

    public sealed class PrivateMissionProfile
    {
        public string PreferredName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string BirthDate { get; set; }
        public bool IsConfigured { get { return !string.IsNullOrWhiteSpace(PreferredName) || !string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName) || !string.IsNullOrWhiteSpace(BirthDate); } }
    }

    public sealed class MissionConfig
    {
        public List<string> EnabledSkills { get; set; }
        public PrivateMissionProfile PrivateProfile { get; set; }
        public static MissionConfig Default() { return new MissionConfig { EnabledSkills = new List<string> { "math.basic_operations_1.addition", "math.basic_operations_1.subtraction", "math.basic_operations_1.multiplication" }, PrivateProfile = new PrivateMissionProfile() }; }
    }

    public sealed class MissionRotationState
    {
        public string LocalDate { get; set; }
        public List<string> UsedSkillsInCycle { get; set; }
        public Dictionary<string, string> LastVariantBySkill { get; set; }
        public MissionRotationState() { UsedSkillsInCycle = new List<string>(); LastVariantBySkill = new Dictionary<string, string>(); }
    }

    public static class GuardianClock
    {
        public static Func<DateTime> LocalNowProvider = delegate { return DateTime.Now; };
        public static DateTime TodayLocal { get { return LocalNowProvider().Date; } }
    }

    public static class MissionText
    {
        public static string Normalize(string value)
        {
            var text = (value ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var result = new StringBuilder(); bool space = false;
            foreach (char c in text)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c)) { result.Append(c); space = false; }
                else if (char.IsWhiteSpace(c) && !space) { result.Append(' '); space = true; }
            }
            return result.ToString().Trim();
        }
    }

    public static class MissionTelemetry
    {
        public static Dictionary<string, object> Payload(Mission m, int attempt)
        {
            return new Dictionary<string, object> { { "mission_id", m.Id }, { "missionId", m.Id }, { "category_id", m.CategoryId }, { "level_id", m.LevelId }, { "skill_id", m.SkillId }, { "variant_id", m.VariantId }, { "attempt", attempt } };
        }
    }

    public sealed class MissionUnavailableDeduplicator
    {
        private bool _unavailable;
        private string _signature;

        public bool ShouldLog(bool hasAvailableMission, string availabilitySignature)
        {
            if (hasAvailableMission)
            {
                _unavailable = false;
                _signature = null;
                return false;
            }
            if (!_unavailable || !string.Equals(_signature, availabilitySignature ?? "", StringComparison.Ordinal))
            {
                _unavailable = true;
                _signature = availabilitySignature ?? "";
                return true;
            }
            return false;
        }
    }

    public sealed class MissionSelector
    {
        private readonly GuardianConfig _config; private readonly MissionCatalog _catalog; private readonly Random _random = new Random();
        public MissionSelector(GuardianConfig config, MissionCatalog catalog) { _config = config; _catalog = catalog; }
        public Mission Next()
        {
            var state = _config.MissionRotationState; var today = GuardianClock.TodayLocal.ToString("yyyy-MM-dd");
            if (state.LocalDate != today) { state.LocalDate = today; state.UsedSkillsInCycle.Clear(); }
            var effective = _catalog.EffectiveSkills(_config.MissionConfig);
            if (effective.Count == 0) { _config.Save(); return null; }
            state.UsedSkillsInCycle.RemoveAll(delegate(string x) { return !effective.Contains(x); });
            var candidates = effective.FindAll(delegate(string x) { return !state.UsedSkillsInCycle.Contains(x); });
            if (candidates.Count == 0) { state.UsedSkillsInCycle.Clear(); candidates.AddRange(effective); }
            var key = candidates[_random.Next(candidates.Count)];
            var mission = _catalog.Generate(key, _config.MissionConfig.PrivateProfile, state.LastVariantBySkill, _random);
            if (mission == null) return null;
            state.UsedSkillsInCycle.Add(key); state.LastVariantBySkill[key] = mission.VariantId; _config.Save(); return mission;
        }
    }

    public sealed class MissionCatalog
    {
        private static readonly string[] Weekdays = { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" };
        private static readonly string[] Months = { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };
        private static readonly string[] Seasons = { "invierno", "primavera", "verano", "otoño" };
        public List<string> EffectiveSkills(MissionConfig config)
        {
            var result = new List<string>(); if (config == null || config.EnabledSkills == null) return result;
            foreach (var key in config.EnabledSkills) if (CanGenerate(key, config.PrivateProfile)) result.Add(key); return result;
        }
        public bool CanGenerate(string key, PrivateMissionProfile p)
        {
            if (key == "comprehension.functional_1.identity") return p != null && (!string.IsNullOrWhiteSpace(p.PreferredName) || !string.IsNullOrWhiteSpace(p.FirstName) || !string.IsNullOrWhiteSpace(p.LastName));
            if (key == "comprehension.functional_1.age_birth") return p != null && ParseDate(p.BirthDate).HasValue;
            return key == "math.basic_operations_1.addition" || key == "math.basic_operations_1.subtraction" || key == "math.basic_operations_1.multiplication" || key == "comprehension.functional_1.current_date" || key == "comprehension.functional_1.temporal_relations" || key == "comprehension.functional_1.calendar" || key == "comprehension.functional_1.seasons";
        }
        public Mission Generate(string key, PrivateMissionProfile p, Dictionary<string, string> last, Random r)
        {
            if (!CanGenerate(key, p)) return null;
            if (key.StartsWith("math.")) return MathMission(key, r);
            if (key.EndsWith(".identity")) return Identity(p, last, r);
            if (key.EndsWith(".age_birth")) return AgeBirth(p, last, r);
            if (key.EndsWith(".current_date")) return CurrentDate(last, r);
            if (key.EndsWith(".temporal_relations")) return Temporal(last, r);
            if (key.EndsWith(".calendar")) return Calendar(last, r);
            return Season(last, r);
        }
        private static Mission M(string cat, string level, string skill, string variant, string prompt, params string[] answers) { return new Mission { Id = Guid.NewGuid().ToString(), CategoryId = cat, LevelId = level, SkillId = skill, VariantId = variant, Prompt = prompt, AcceptedAnswers = new List<string>(answers) }; }
        private Mission MathMission(string key, Random r)
        {
            var skill = key.Substring(key.LastIndexOf('.') + 1); int a, b, answer; string symbol;
            if (skill == "addition") { a = r.Next(20, 100); b = r.Next(10, 90); answer = a + b; symbol = "+"; }
            else if (skill == "subtraction") { a = r.Next(40, 130); b = r.Next(10, Math.Min(90, a)); answer = a - b; symbol = "-"; }
            else { a = r.Next(3, 13); b = r.Next(3, 13); answer = a * b; symbol = "x"; }
            return M("math", "basic_operations_1", skill, "generated", a + " " + symbol + " " + b + " = ?", answer.ToString());
        }
        private Mission Identity(PrivateMissionProfile p, Dictionary<string, string> last, Random r)
        {
            var full = Join(p.FirstName, p.MiddleName, p.LastName); var firstLast = Join(p.FirstName, p.LastName);
            return Choose(new List<Mission> { M("comprehension","functional_1","identity","identity_name_ask_1","¿Cuál es tu nombre?", NonEmpty(p.PreferredName,p.FirstName,firstLast,full).ToArray()), M("comprehension","functional_1","identity","identity_name_ask_2","¿Cómo te llamás?", NonEmpty(p.PreferredName,p.FirstName,firstLast,full).ToArray()), M("comprehension","functional_1","identity","identity_name_field","Nombre:", NonEmpty(p.PreferredName,p.FirstName,firstLast,full).ToArray()), M("comprehension","functional_1","identity","identity_last_name_ask","¿Cuál es tu apellido?", NonEmpty(p.LastName).ToArray()), M("comprehension","functional_1","identity","identity_last_name_field","Apellido:", NonEmpty(p.LastName).ToArray()), M("comprehension","functional_1","identity","identity_name_last_name_ask","¿Cuál es tu nombre y apellido?", NonEmpty(firstLast,full).ToArray()), M("comprehension","functional_1","identity","identity_name_last_name_field","Nombre y apellido:", NonEmpty(firstLast,full).ToArray()), M("comprehension","functional_1","identity","identity_full_name_ask","¿Cuál es tu nombre completo?", NonEmpty(full).ToArray()) }, last, "comprehension.functional_1.identity", r);
        }
        private Mission AgeBirth(PrivateMissionProfile p, Dictionary<string, string> last, Random r)
        {
            var birth = ParseDate(p.BirthDate).Value; var today = GuardianClock.TodayLocal; var age = today.Year - birth.Year; if (birth > today.AddYears(-age)) age--;
            return Choose(new List<Mission> { M("comprehension","functional_1","age_birth","age_ask_1","¿Cuántos años tenés?", NumberAnswers(age).ToArray()), M("comprehension","functional_1","age_birth","age_ask_2","¿Qué edad tenés?", NumberAnswers(age).ToArray()), M("comprehension","functional_1","age_birth","age_field","Edad:", NumberAnswers(age).ToArray()), M("comprehension","functional_1","age_birth","birth_year_ask","¿En qué año naciste?",birth.Year.ToString()), M("comprehension","functional_1","age_birth","birth_year_field","Año de nacimiento:",birth.Year.ToString()), M("comprehension","functional_1","age_birth","birthday_ask","¿Cuándo es tu cumpleaños?", DateAnswers(birth, false).ToArray()) }, last, "comprehension.functional_1.age_birth", r);
        }
        private Mission CurrentDate(Dictionary<string, string> last, Random r) { var d = GuardianClock.TodayLocal; return Choose(new List<Mission> { M("comprehension","functional_1","current_date","current_year_ask_1","¿En qué año estamos?",d.Year.ToString()), M("comprehension","functional_1","current_date","current_year_ask_2","¿Qué año es?",d.Year.ToString()), M("comprehension","functional_1","current_date","current_month_ask_1","¿En qué mes estamos?",Months[d.Month-1]), M("comprehension","functional_1","current_date","current_month_ask_2","¿Qué mes es?",Months[d.Month-1]), M("comprehension","functional_1","current_date","current_weekday","¿Qué día de la semana es hoy?",Weekdays[(int)d.DayOfWeek]), M("comprehension","functional_1","current_date","current_day_of_month","¿Qué día del mes es hoy?",d.Day.ToString()), M("comprehension","functional_1","current_date","current_full_date","¿Qué fecha es hoy?",DateAnswers(d,true).ToArray()) },last,"comprehension.functional_1.current_date",r); }
        private Mission Temporal(Dictionary<string, string> last, Random r) { var d = GuardianClock.TodayLocal; return Choose(new List<Mission> { M("comprehension","functional_1","temporal_relations","tomorrow_weekday","¿Qué día de la semana es mañana?",Weekdays[(int)d.AddDays(1).DayOfWeek]), M("comprehension","functional_1","temporal_relations","yesterday_weekday","¿Qué día de la semana fue ayer?",Weekdays[(int)d.AddDays(-1).DayOfWeek]), M("comprehension","functional_1","temporal_relations","next_month_ask_1","¿Cuál es el mes que viene?",Months[d.AddMonths(1).Month-1]), M("comprehension","functional_1","temporal_relations","next_month_ask_2","¿Qué mes viene después de este?",Months[d.AddMonths(1).Month-1]), M("comprehension","functional_1","temporal_relations","previous_month","¿Cuál fue el mes pasado?",Months[d.AddMonths(-1).Month-1]) },last,"comprehension.functional_1.temporal_relations",r); }
        private Mission Calendar(Dictionary<string, string> last, Random r) { var day=r.Next(7); var month=r.Next(12); return Choose(new List<Mission> { M("comprehension","functional_1","calendar","days_in_week","¿Cuántos días tiene una semana?",NumberAnswers(7).ToArray()), M("comprehension","functional_1","calendar","months_in_year","¿Cuántos meses tiene un año?",NumberAnswers(12).ToArray()), M("comprehension","functional_1","calendar","weekday_after","¿Qué día viene después del "+Weekdays[day]+"?",Weekdays[(day+1)%7]), M("comprehension","functional_1","calendar","weekday_before","¿Qué día viene antes del "+Weekdays[day]+"?",Weekdays[(day+6)%7]), M("comprehension","functional_1","calendar","month_after","¿Qué mes viene después de "+Months[month]+"?",Months[(month+1)%12]), M("comprehension","functional_1","calendar","month_before","¿Qué mes viene antes de "+Months[month]+"?",Months[(month+11)%12]) },last,"comprehension.functional_1.calendar",r); }
        private Mission Season(Dictionary<string, string> last, Random r) { var s=r.Next(4); return Choose(new List<Mission> { M("comprehension","functional_1","seasons","season_cold","¿Cuál es la estación del año en la que hace mucho frío?","invierno"), M("comprehension","functional_1","seasons","season_hot","¿Cuál es la estación del año en la que hace mucho calor?","verano"), M("comprehension","functional_1","seasons","season_falling_leaves","¿En qué estación se caen muchas hojas de los árboles?","otoño"), M("comprehension","functional_1","seasons","season_flowers","¿En qué estación suelen crecer muchas flores?","primavera"), M("comprehension","functional_1","seasons","season_after","¿Qué estación viene después del "+Seasons[s]+"?",Seasons[(s+1)%4]) },last,"comprehension.functional_1.seasons",r); }
        private static Mission Choose(List<Mission> list, Dictionary<string,string> last, string skill, Random r) { list.RemoveAll(delegate(Mission x){return x.AcceptedAnswers.Count==0;}); var prior=last!=null&&last.ContainsKey(skill)?last[skill]:""; var choices=list.FindAll(delegate(Mission x){return list.Count==1||x.VariantId!=prior;}); return choices[r.Next(choices.Count)]; }
        private static List<string> NonEmpty(params string[] values) { var result=new List<string>(); foreach(var x in values) if(!string.IsNullOrWhiteSpace(x)) result.Add(x); return result; }
        private static string Join(params string[] values) { return string.Join(" ", NonEmpty(values).ToArray()); }
        private static DateTime? ParseDate(string text) { DateTime date; return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ? date.Date : (DateTime?)null; }
        private static List<string> NumberAnswers(int n) { var a=new List<string>{n.ToString()}; if(n==7)a.Add("siete"); if(n==12)a.Add("doce"); return a; }
        private static List<string> DateAnswers(DateTime d, bool year) { var a=new List<string>{d.ToString(year?"dd/MM/yyyy":"dd/MM"),d.Day+" de "+Months[d.Month-1]+(year?" de "+d.Year:"")}; if(year)a.Add(d.Day+" "+Months[d.Month-1]+" "+d.Year); return a; }
    }
}
