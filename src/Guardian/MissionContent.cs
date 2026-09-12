using System;
using System.Collections.Generic;

namespace Guardian
{
    // Fuente editable de textos pedagógicos y de UI. La lógica de selección y validación permanece fuera de este archivo.
    public static class MissionContent
    {
        public const string RephraseButton = "Decilo de otra manera";
        public const string HintButton = "Dame una pista";
        public const string GuidedButton = "Guiame un poco más";
        public const string NoAttemptFeedback = "LEÉ la pregunta y PENSÁ qué te está pidiendo. Vos podés.";
        public const string NumberRequiredFeedback = "La respuesta es un NÚMERO.";

        private static readonly string[] Weekdays = { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" };
        private static readonly string[] Months = { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };
        private static readonly string[] Seasons = { "invierno", "primavera", "verano", "otoño" };
        private static readonly string[] KeyboardRows = { "qwertyuiop", "asdfghjkl", "zxcvbnm" };

        public static string WritingFeedback(WritingDifference difference)
        {
            if (difference == WritingDifference.ExtraLetter) return "Parece que hay una letra de más. Leé cómo lo escribiste.";
            if (difference == WritingDifference.MissingLetter) return "Parece que falta una letra. Leé cómo lo escribiste.";
            if (difference == WritingDifference.TransposedLetters) return "Parece que dos letras están en otro orden. Leé cómo lo escribiste.";
            if (difference == WritingDifference.SubstitutedLetter) return "Parece que hay una letra que no va. Leé cómo lo escribiste.";
            return "Leé cómo lo escribiste.";
        }

        public static string WritingAnswerRevealed(string answer) { return "Se escribe: " + answer + ". Ahora escribilo vos correctamente."; }

        // Una respuesta que no parece una palabra no consume un intento ni abre ayudas.
        // Las palabras reales de las misiones pueden escribirse con errores, por eso el
        // corrector de la respuesta esperada se evalúa antes de llegar a este método.
        public static MissionFeedback FeedbackForWrongAnswer(Mission mission, string answer, ISet<string> priorCandidates)
        {
            if (mission == null || string.Equals(mission.CategoryId, "math", StringComparison.OrdinalIgnoreCase)) return new MissionFeedback { Kind = MissionFeedbackKind.None };

            var normalized = MissionText.Normalize(answer);
            if (LooksLikeNoAttempt(normalized)) return new MissionFeedback { Kind = MissionFeedbackKind.NoAttempt, Text = NoAttemptFeedback };

            string category;
            string candidate;
            bool candidateHasSpellingError;
            if (!TryKnownCalendarCandidate(normalized, out category, out candidate, out candidateHasSpellingError)) return new MissionFeedback { Kind = MissionFeedbackKind.None };

            var candidateKey = category + ":" + MissionText.Normalize(candidate);
            var expectedCategory = ExpectedCalendarCategory(mission);
            if (priorCandidates != null && priorCandidates.Contains(candidateKey))
                return new MissionFeedback { Kind = MissionFeedbackKind.NoAttempt, Text = NoAttemptFeedback, CandidateKey = candidateKey };
            if (expectedCategory == null || expectedCategory != category)
                return new MissionFeedback { Kind = MissionFeedbackKind.Contextual, Text = ContextualText(answer, candidate, category, candidateHasSpellingError, ExpectedCalendarDescription(mission)), CandidateKey = candidateKey };

            if (priorCandidates != null && !priorCandidates.Contains(candidateKey) && HasPriorCandidateInCategory(priorCandidates, category))
                return new MissionFeedback { Kind = MissionFeedbackKind.Contextual, Text = ContextualText(answer, candidate, category, candidateHasSpellingError, ExpectedCalendarDescription(mission)), CandidateKey = candidateKey };

            return new MissionFeedback { Kind = MissionFeedbackKind.None, CandidateKey = candidateKey };
        }

        private static bool HasPriorCandidateInCategory(ISet<string> candidates, string category)
        {
            var prefix = category + ":";
            foreach (var candidate in candidates) if (candidate.StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
        }

        private static string ContextualText(string answer, string candidate, string category, bool candidateHasSpellingError, string requested)
        {
            var namedCandidate = char.ToUpper(candidate[0]) + candidate.Substring(1);
            var categoryDescription = category == "weekday" ? "un día de la semana" : category == "month" ? "un mes del año" : "una estación del año";
            if (candidateHasSpellingError) return "Quisiste decir " + candidate + ". " + namedCandidate + " es " + categoryDescription + ", pero te pide " + requested + ".";
            var written = (answer ?? "").Trim();
            return "Pusiste " + written + ". " + namedCandidate + " es " + categoryDescription + ", pero te pide " + requested + ".";
        }

        private static bool LooksLikeNoAttempt(string normalized)
        {
            if (string.IsNullOrWhiteSpace(normalized)) return true;
            if (normalized.IndexOf(' ') >= 0 || normalized.Length < 3) return false;
            var allSame = true;
            for (var i = 1; i < normalized.Length; i++) if (normalized[i] != normalized[0]) { allSame = false; break; }
            if (allSame) return true;
            foreach (var row in KeyboardRows) if (row.IndexOf(normalized, StringComparison.Ordinal) >= 0 || Reverse(row).IndexOf(normalized, StringComparison.Ordinal) >= 0) return true;
            foreach (var c in normalized) if ("aeiouáéíóú".IndexOf(c) >= 0) return false;
            return true;
        }

        private static string Reverse(string value) { var chars = value.ToCharArray(); Array.Reverse(chars); return new string(chars); }

        private static bool TryKnownCalendarCandidate(string normalized, out string category, out string candidate, out bool candidateHasSpellingError)
        {
            category = null; candidate = null; candidateHasSpellingError = false;
            if (TryCandidate(normalized, Seasons, out candidate, out candidateHasSpellingError)) { category = "season"; return true; }
            if (TryCandidate(normalized, Months, out candidate, out candidateHasSpellingError)) { category = "month"; return true; }
            if (TryCandidate(normalized, Weekdays, out candidate, out candidateHasSpellingError)) { category = "weekday"; return true; }
            return false;
        }

        private static bool TryCandidate(string normalized, string[] values, out string candidate, out bool candidateHasSpellingError)
        {
            candidate = null; candidateHasSpellingError = false;
            foreach (var value in values)
            {
                var expected = MissionText.Normalize(value);
                if (normalized == expected) { candidate = value; return true; }
                var limit = expected.Length <= 5 ? 1 : 2;
                if (normalized.Length >= 3 && MissionValidator.DamerauLevenshtein(normalized, expected) <= limit) { candidate = value; candidateHasSpellingError = true; return true; }
            }
            return false;
        }

        private static string ExpectedCalendarCategory(Mission mission)
        {
            var id = mission.VariantId ?? "";
            if (id == "current_month_ask_1" || id == "current_month_ask_2" || id == "next_month_ask_1" || id == "previous_month" || id == "month_after" || id == "month_before" || id == "vocab_after") return "month";
            if (id == "current_weekday" || id == "tomorrow_weekday" || id == "yesterday_weekday" || id == "weekday_after" || id == "weekday_before" || id == "vocab_before" || id == "explicit_when_doctor" || id == "explicit_when_party") return "weekday";
            if (id.IndexOf("season_", StringComparison.Ordinal) == 0) return "season";
            return null;
        }

        private static string ExpectedCalendarDescription(Mission mission)
        {
            var id = mission.VariantId ?? "";
            var value = mission.ContentContext == null ? "" : mission.ContentContext.Value;
            if (id == "current_month_ask_1" || id == "current_month_ask_2") return "el mes en el que estamos";
            if (id == "age_ask_1" || id == "age_ask_2" || id == "age_field") return "tu edad";
            if (id == "birth_year_ask" || id == "birth_year_field") return "el año en el que naciste";
            if (id == "birthday_ask") return "la fecha de tu cumpleaños";
            if (id == "birth_date_ask") return "tu fecha de nacimiento";
            if (id == "current_year_ask_1" || id == "current_year_ask_2") return "el año en el que estamos";
            if (id == "current_day_of_month") return "el número del día del mes";
            if (id == "current_full_date") return "la fecha de hoy";
            if (id == "next_month_ask_1") return "el mes que viene";
            if (id == "previous_month") return "el mes pasado";
            if (id == "month_after") return "el mes que sigue a " + value;
            if (id == "month_before") return "el mes que está antes de " + value;
            if (id == "vocab_after") return "el mes que está después de febrero";
            if (id == "current_weekday") return "el día de la semana de hoy";
            if (id == "tomorrow_weekday") return "el día de la semana de mañana";
            if (id == "yesterday_weekday") return "el día de la semana de ayer";
            if (id == "weekday_after") return "el día que sigue a " + value;
            if (id == "weekday_before") return "el día que está antes de " + value;
            if (id == "vocab_before") return "el día que está antes de miércoles";
            if (id == "explicit_when_doctor") return "el día del turno";
            if (id == "explicit_when_party") return "el día de la fiesta";
            if (id == "location_city_ask_1" || id == "location_city_ask_2") return "una ciudad";
            if (id == "location_province_ask_1" || id == "location_province_ask_2" || id == "location_city_to_province") return "una provincia";
            if (id == "location_country_ask_1" || id == "location_country_ask_2" || id == "location_province_to_country" || id == "location_city_to_country") return "un país";
            if (id == "season_cold") return "la estación en la que hace mucho frío";
            if (id == "season_hot") return "la estación en la que hace mucho calor";
            if (id == "season_falling_leaves") return "la estación en la que se caen muchas hojas";
            if (id == "season_flowers") return "la estación en la que suelen crecer muchas flores";
            if (id == "season_after") return "la estación que sigue a " + value;
            return "la respuesta de la pregunta";
        }

        // Prompts is the runtime source for every comprehension VariantId. Dynamic {0} values are supplied by MissionSystem.
        private static readonly Dictionary<string, string> Prompts = new Dictionary<string, string> {
            { "identity_name_ask_1", "¿Cuál es tu nombre?" }, { "identity_name_ask_2", "¿Cómo te llamás?" }, { "identity_name_field", "Nombre:" }, { "identity_last_name_ask", "¿Cuál es tu apellido?" }, { "identity_last_name_field", "Apellido:" }, { "identity_name_last_name_ask", "¿Cuál es tu nombre y apellido?" }, { "identity_name_last_name_field", "Nombre y apellido:" }, { "identity_full_name_ask", "¿Cuál es tu nombre completo?" },
            { "age_ask_1", "¿Cuántos años tenés?" }, { "age_ask_2", "¿Qué edad tenés?" }, { "age_field", "Edad:" }, { "birth_year_ask", "¿En qué año naciste?" }, { "birth_year_field", "Año de nacimiento:" }, { "birthday_ask", "¿Cuándo es tu cumpleaños?" }, { "birth_date_ask", "¿Cuál es tu fecha de nacimiento?" },
            { "current_year_ask_1", "¿En qué año estamos?" }, { "current_year_ask_2", "¿Qué año es?" }, { "current_month_ask_1", "¿En qué mes estamos?" }, { "current_month_ask_2", "¿Qué mes es?" }, { "current_weekday", "¿Qué día de la semana es hoy?" }, { "current_day_of_month", "¿Qué día del mes es hoy?" }, { "current_full_date", "¿Qué fecha es hoy?" },
            { "tomorrow_weekday", "¿Qué día de la semana es mañana?" }, { "yesterday_weekday", "¿Qué día de la semana fue ayer?" }, { "next_month_ask_1", "¿Cuál es el mes que viene?" }, { "previous_month", "¿Cuál fue el mes pasado?" },
            { "days_in_week", "¿Cuántos días tiene una semana?" }, { "months_in_year", "¿Cuántos meses tiene un año?" }, { "weekday_after", "¿Qué día de la semana viene después del {0}?" }, { "weekday_before", "¿Qué día viene antes del {0}?" }, { "month_after", "¿Qué mes viene después de {0}?" }, { "month_before", "¿Qué mes viene antes de {0}?" },
            { "season_cold", "¿Cuál es la estación del año en la que hace mucho frío?" }, { "season_hot", "¿Cuál es la estación del año en la que hace mucho calor?" }, { "season_falling_leaves", "¿En qué estación se caen muchas hojas de los árboles?" }, { "season_flowers", "¿En qué estación suelen crecer muchas flores?" }, { "season_after", "¿Qué estación viene después del {0}?" },
            { "vocab_how_many", "⭐⭐⭐⭐ ¿Cuántas estrellas hay?" }, { "vocab_quantity", "Hay 3 lápices. ¿Cuántos lápices hay?" }, { "vocab_before", "Lunes, martes, miércoles. ¿Qué día está antes de miércoles?" }, { "vocab_after", "Enero, febrero, marzo. ¿Qué mes está después de febrero?" }, { "vocab_next", "Uno, dos, tres... ¿qué número es el siguiente?" }, { "vocab_previous", "Uno, dos, tres... ¿qué número es el anterior a tres?" }, { "vocab_first", "Rojo, azul, verde. ¿Cuál está primero?" }, { "vocab_last", "Rojo, azul, verde. ¿Cuál está último?" },
            { "add_strawberries", "Hay {0} frutillas en casa. Papá compra {1} frutillas más. ¿Cuántas frutillas hay en total?" }, { "add_stickers", "{0} figuritas están en el álbum. Agregás {1} figuritas. ¿Cuántas figuritas hay en el álbum?" }, { "add_pencils", "En la cartuchera hay {0} lápices. Guardás {1} lápices más. ¿Cuántos lápices hay en total?" }, { "add_balloons", "Hay {0} globos en la mesa. Traen {1} globos más. ¿Cuántos globos hay en total?" }, { "add_cookies", "Hay {0} galletitas en el plato. Ponés {1} galletitas más. ¿Cuántas galletitas hay en total?" }, { "add_blocks", "Tenés {0} bloques. Te regalan {1} bloques. ¿Cuántos bloques tenés en total?" },
            { "subtract_strawberries", "Hay {0} frutillas en casa. Comen {1}. ¿Cuántas frutillas quedan?" }, { "subtract_stickers", "Tenés {0} figuritas. Regalás {1}. ¿Cuántas figuritas te quedan?" }, { "subtract_pencils", "Tenés {0} lápices. Perdés {1}. ¿Cuántos lápices te quedan?" }, { "subtract_balloons", "Hay {0} globos en la mesa. Se pinchan {1}. ¿Cuántos globos quedan?" }, { "subtract_cookies", "Hay {0} galletitas en el plato. Comés {1}. ¿Cuántas galletitas quedan?" }, { "subtract_blocks", "Tenés {0} bloques tirados en el piso. Guardás {1} en una caja. ¿Cuántos bloques te faltan guardar?" },
            { "location_city_ask_1", "¿En qué ciudad vivís?" }, { "location_city_ask_2", "¿Cuál es la ciudad donde vivís?" }, { "location_province_ask_1", "¿En qué provincia vivís?" }, { "location_province_ask_2", "¿Cuál es la provincia donde vivís?" }, { "location_country_ask_1", "¿En qué país vivís?" }, { "location_country_ask_2", "¿Cuál es el país donde vivís?" }, { "location_city_to_province", "¿En qué provincia se encuentra {0}?" }, { "location_province_to_country", "¿En qué país se encuentra {0}?" }, { "location_city_to_country", "¿En qué país se encuentra {0}?" },
            { "explicit_color_balloon", "Emma tiene un globo rojo. ¿De qué color es el globo?" }, { "explicit_when_doctor", "Matías tiene turno con la doctora el miércoles. ¿Cuándo tiene turno Matías con la doctora?" }, { "explicit_when_party", "La fiesta de Ana es el sábado. ¿Cuándo es la fiesta de Ana?" }, { "explicit_owner_ball", "Tomás tiene una pelota. ¿Quién tiene la pelota?" }, { "explicit_owner_book", "Lucía tiene un libro. ¿Quién tiene el libro?" }, { "explicit_location_cup", "La taza está en la mesa. ¿Dónde está la taza?" }, { "explicit_location_ball", "La pelota está debajo de la silla. ¿Dónde está la pelota?" }, { "explicit_quantity_cats", "Hay 3 gatos en el patio. ¿Cuántos gatos hay?" }, { "explicit_quantity_pencils", "Hay 4 lápices en la caja. ¿Cuántos lápices hay?" }, { "explicit_action_nina", "Nina dibuja una flor. ¿Qué dibuja Nina?" }, { "explicit_action_mateo", "Mateo come una manzana. ¿Qué come Mateo?" }, { "explicit_object_dog", "El perro duerme en su cama. ¿Dónde duerme el perro?" }
        };

        public static string PromptFor(string variantId, params object[] values)
        {
            string template;
            if (!Prompts.TryGetValue(variantId, out template)) throw new InvalidOperationException("Falta contenido para la variante " + variantId + ".");
            return values == null || values.Length == 0 ? template : string.Format(template, values);
        }

        public static List<MissionHelpStep> HelpSteps(Mission mission)
        {
            string one = null;
            string two = null;
            string three = null;
            var id = mission.VariantId;

            if (id == "identity_name_ask_1") { one="¿Qué es tu NOMBRE?"; two="¿Te está preguntando tu nombre o tu apellido?"; three="¿Cómo te llama tu mamá?"; }
            else if (id == "identity_name_ask_2") { one="Cuando alguien pregunta CÓMO TE LLAMÁS, ¿qué quiere saber?"; two="Te está preguntando cuál es tu nombre."; three="¿Cómo te llama tu mamá?"; }
            else if (id == "identity_name_field") { one="¿Qué significa NOMBRE?"; two="Escribí cómo te llamás."; three=NicknameOrFallback(mission.ContentContext); }
            else if (id == "identity_last_name_ask" || id == "identity_last_name_field") { one="¿Qué significa APELLIDO?"; two="Pensá que tu papá tiene el mismo apellido."; three="Es la última parte de tu nombre completo."; }
            else if (id == "identity_name_last_name_ask" || id == "identity_name_last_name_field") { one="¿Qué significa NOMBRE Y APELLIDO?"; two="Escribí cómo te llamás y después tu apellido."; three="Primero escribí tu nombre y después tu apellido."; }
            else if (id == "identity_full_name_ask") { one="¿Qué significa NOMBRE COMPLETO?"; two="Pensá en todos los nombres que forman tu nombre completo."; three="Escribí tu nombre, segundo nombre y apellido."; }
            else if (id == "age_ask_1") { one="¿Qué significa CUÁNTOS?"; two="Cuántos pregunta por una cantidad. ¿Qué cantidad de años tenés?"; three="¿Cuántos años cumpliste en tu último cumpleaños?"; }
            else if (id == "age_ask_2" || id == "age_field") { one="¿Qué significa EDAD?"; two="Edad quiere decir cuántos años tiene una persona."; three="¿Cuántos años tenés?"; }
            else if (id == "birth_year_ask" || id == "birth_year_field") { one="¿Qué significa AÑO?"; two="Pensá en tu fecha de nacimiento. Tiene día, mes y año."; three="De esa fecha, escribí solamente el año en que naciste."; }
            else if (id == "birthday_ask") { one="Cuando pregunta CUÁNDO, ¿qué dato quiere saber?"; two="¿En qué día y mes cumplís años?"; three="Pensá en el día y el mes de tu cumpleaños."; }
            else if (id == "birth_date_ask") { one="¿Qué significa FECHA DE NACIMIENTO?"; two="Es la fecha del día en que naciste: pensá en el día, el mes y el año."; three="Escribí el número del día, el nombre del mes y el año en que naciste."; }
            else if (id == "current_year_ask_1" || id == "current_year_ask_2") { one="¿Qué significa AÑO?"; two="Cuando escribís la fecha de hoy escribís día, mes y año. ¿Cuál es el año?"; three="Escribí el número de cuatro cifras del año en el que estamos."; }
            else if (id == "current_month_ask_1" || id == "current_month_ask_2") { one="¿Qué significa MES?"; two="La pregunta quiere saber en qué mes estamos ahora."; three="Pensá en los meses: enero, febrero, marzo... ¿cuál es el de ahora?"; }
            else if (id == "current_weekday") { one="¿Qué significa DÍA DE LA SEMANA?"; two="Te pregunta cómo se llama el día de hoy."; three="Los días son lunes, martes, miércoles, jueves, viernes, sábado y domingo. ¿Cuál es hoy?"; }
            else if (id == "current_day_of_month") { one="¿A qué se refiere con DÍA DEL MES?"; two="Pensá en el número del día de hoy."; three="Si escribís la fecha de hoy, ¿qué número escribís primero?"; }
            else if (id == "current_full_date") { one="¿Qué significa FECHA?"; two="Pensá en qué día, mes y año estamos."; three="Escribí el número del día, el nombre del mes y el año de hoy."; }
            else if (id == "tomorrow_weekday") { one="¿Qué significa MAÑANA? ¿Qué significa DÍA DE LA SEMANA?"; two="Pensá qué día es hoy y cuál viene después."; three="Hoy es {todayWeekday}. ¿Qué día viene después?"; }
            else if (id == "yesterday_weekday") { one="¿Qué significa AYER? ¿Qué significa DÍA DE LA SEMANA?"; two="Pensá qué día es hoy y cuál estuvo antes."; three="Hoy es {todayWeekday}. ¿Qué día fue ayer?"; }
            else if (id == "next_month_ask_1") { one="¿Qué significa MES QUE VIENE?"; two="Pensá qué mes es ahora y cuál viene después."; three="Ahora estamos en {currentMonth}. ¿Qué mes viene después?"; }
            else if (id == "previous_month") { one="¿Qué significa MES PASADO?"; two="Pensá qué mes es ahora y cuál estuvo antes."; three="Ahora estamos en {currentMonth}. ¿Qué mes estuvo antes?"; }
            else if (id == "days_in_week") { one="¿Qué significa CUÁNTOS?"; two="Cuántos pregunta por una cantidad. ¿Qué cantidad de días tiene una semana?"; three="Contá: lunes, martes, miércoles, jueves, viernes, sábado y domingo. ¿Cuántos días son?"; }
            else if (id == "months_in_year") { one="¿Qué significa CUÁNTOS?"; two="Cuántos pregunta por una cantidad. ¿Qué cantidad de meses tiene un año?"; three="Contá: enero, febrero, marzo, abril, mayo, junio, julio, agosto, septiembre, octubre, noviembre y diciembre. ¿Cuántos meses son?"; }
            else if (id == "weekday_after") { one="¿Qué significa DESPUÉS?"; two="Pensá qué día viene después de {0}."; three="Pensá los días en orden: lunes, martes, miércoles, jueves, viernes, sábado y domingo. Buscá {0} y elegí el que sigue."; }
            else if (id == "weekday_before") { one="¿Qué significa ANTES?"; two="Pensá qué día está antes de {0}."; three="Pensá los días en orden: lunes, martes, miércoles, jueves, viernes, sábado y domingo. Buscá {0} y elegí el anterior."; }
            else if (id == "month_after") { one="¿Qué significa DESPUÉS?"; two="Pensá qué mes viene después de {0}."; three="Pensá los meses en orden: enero, febrero, marzo, abril, mayo, junio, julio, agosto, septiembre, octubre, noviembre y diciembre. Buscá {0} y elegí el que sigue."; }
            else if (id == "month_before") { one="¿Qué significa ANTES?"; two="Pensá qué mes está antes de {0}."; three="Pensá los meses en orden: enero, febrero, marzo, abril, mayo, junio, julio, agosto, septiembre, octubre, noviembre y diciembre. Buscá {0} y elegí el anterior."; }
            else if (id == "season_cold") { one="¿Cuáles son las ESTACIONES DEL AÑO?"; two="Pensá en la estación en la que usamos campera y gorro."; three="Las estaciones son verano, otoño, invierno y primavera. ¿Cuál asociás al frío?"; }
            else if (id == "season_hot") { one="¿Cuáles son las ESTACIONES DEL AÑO?"; two="Pensá en la estación en la que vamos a la pileta."; three="Las estaciones son verano, otoño, invierno y primavera. ¿Cuál asociás al calor?"; }
            else if (id == "season_falling_leaves") { one="¿Cuáles son las ESTACIONES DEL AÑO?"; two="Pensá en cuándo muchas hojas cambian de color y caen."; three="Las estaciones son verano, otoño, invierno y primavera. ¿En cuál pasa eso?"; }
            else if (id == "season_flowers") { one="¿Cuáles son las ESTACIONES DEL AÑO?"; two="Pensá en cuándo empiezan a aparecer muchas flores."; three="Las estaciones son verano, otoño, invierno y primavera. ¿En cuál pasa eso?"; }
            else if (id == "season_after") { one="¿Qué significa DESPUÉS?"; two="Pensá en el orden de las estaciones."; three="El orden es verano → otoño → invierno → primavera → verano. Buscá {0} y elegí la que sigue."; }
            else if (id == "vocab_how_many") { one="¿Qué significa CUÁNTAS?"; two="Cuántas pregunta por una cantidad. ¿Qué cantidad de estrellas hay?"; three="Contá las estrellas y escribí el número."; }
            else if (id == "vocab_quantity") { one="¿Qué significa CUÁNTOS?"; two="Cuántos pregunta por una cantidad. ¿Qué cantidad de lápices hay?"; three="La oración dice: “Hay 3 lápices”. ¿Qué número tenés que escribir?"; }
            else if (id == "vocab_before") { one="¿Qué significa ANTES?"; two="Antes quiere decir el que está justo adelante en este orden."; three="Mirá: lunes → martes → miércoles. ¿Cuál está antes de miércoles?"; }
            else if (id == "vocab_after") { one="¿Qué significa DESPUÉS?"; two="Después quiere decir el que viene justo a continuación."; three="Mirá: enero → febrero → marzo. ¿Cuál está después de febrero?"; }
            else if (id == "vocab_next") { one="¿Qué significa SIGUIENTE?"; two="Siguiente quiere decir el que viene justo después."; three="Completá: 1, 2, 3, ___."; }
            else if (id == "vocab_previous") { one="¿Qué significa ANTERIOR?"; two="Anterior quiere decir el que está justo antes."; three="Completá: 1, ___, 3."; }
            else if (id == "vocab_first") { one="¿Qué significa PRIMERO?"; two="Primero es el que está antes que todos los demás."; three="Mirá: rojo → azul → verde. ¿Cuál está al comienzo?"; }
            else if (id == "vocab_last") { one="¿Qué significa ÚLTIMO?"; two="Último es el que está después de todos los demás."; three="Mirá: rojo → azul → verde. ¿Cuál está al final?"; }
            else if (id == "add_strawberries") { one="La pregunta pide saber cuántas frutillas hay en total."; two="La palabra MÁS avisa que se agregan frutillas."; three="Cuando se agregan más frutillas, se suman: {a} + {b} = ___."; }
            else if (id == "add_stickers") { one="La pregunta pide el total de figuritas que hay en el álbum."; two="AGREGAR significa sumar algo a lo que ya había."; three="Agregar figuritas es sumar: {a} + {b} = ___."; }
            else if (id == "add_pencils") { one="La pregunta pide cuántos lápices hay en total."; two="MÁS indica que entran lápices nuevos."; three="Como entran más lápices, sumá: {a} + {b} = ___."; }
            else if (id == "add_balloons") { one="La pregunta pide el total de globos."; two="La palabra MÁS indica que la cantidad aumenta."; three="Para saber el total cuando llegan más, sumá: {a} + {b} = ___."; }
            else if (id == "add_cookies") { one="La pregunta pide cuántas galletitas hay en total."; two="PONER MÁS hace que haya una cantidad mayor."; three="Al poner más, la cantidad aumenta: {a} + {b} = ___."; }
            else if (id == "add_blocks") { one="La pregunta pide cuántos bloques tenés en total."; two="Un REGALO agrega bloques a los que ya tenías."; three="Un regalo agrega bloques: {a} + {b} = ___."; }
            else if (id == "subtract_strawberries") { one="La pregunta pide saber cuántas frutillas quedan."; two="COMEN indica que se sacan frutillas de las que había."; three="Cuando se sacan frutillas, se resta: {a} − {b} = ___."; }
            else if (id == "subtract_stickers") { one="La pregunta pide las figuritas que quedan después de regalar."; two="REGALAR hace que tengas menos figuritas."; three="Regalar quita figuritas: {a} − {b} = ___."; }
            else if (id == "subtract_pencils") { one="La pregunta pide los lápices que te quedan."; two="PERDER significa que ya no tenés algunos lápices."; three="Los lápices perdidos se sacan de los que tenías: {a} − {b} = ___."; }
            else if (id == "subtract_balloons") { one="La pregunta pide los globos que quedan."; two="SE PINCHAN indica que algunos globos ya no están."; three="Si algunos globos ya no están, restá: {a} − {b} = ___."; }
            else if (id == "subtract_cookies") { one="La pregunta pide cuántas galletitas quedan en el plato."; two="COMER quita galletitas del plato."; three="Comer quita galletitas del plato: {a} − {b} = ___."; }
            else if (id == "subtract_blocks") { one="La pregunta pide los bloques que todavía faltan guardar."; two="GUARDAR algunos bloques deja otros sin guardar."; three="Los que guardaste se sacan de los que había en el piso: {a} − {b} = ___."; }
            else if (id == "location_city_ask_1" || id == "location_city_ask_2") { one="La pregunta pide el nombre de tu ciudad."; two="Recordá cómo se llama la ciudad donde está tu casa."; three="Tu ciudad es {city}. Escribila."; }
            else if (id == "location_province_ask_1" || id == "location_province_ask_2") { one="La pregunta pide el nombre de tu provincia."; two="Recordá cómo se llama la provincia donde está tu ciudad."; three="Tu provincia es {province}. Escribila."; }
            else if (id == "location_country_ask_1" || id == "location_country_ask_2") { one="La pregunta pide el nombre de tu país."; two="Pensá: vivís en la provincia de {province}, que es una de las provincias de tu país."; three="Tu país es {country}. Escribilo."; }
            else if (id == "location_city_to_province") { one="La pregunta pide el nombre de una provincia."; two="Recordá cómo se llama la provincia donde está tu ciudad."; three="{city} está en la provincia de {province}. Escribí {province}."; }
            else if (id == "location_province_to_country") { one="La pregunta pide el nombre de un país."; two="Pensá: {province} es una de las provincias de tu país."; three="{province} está en {country}. Escribí {country}."; }
            else if (id == "location_city_to_country") { one="La pregunta pide el nombre de un país."; two="Pensá: {city} está en la provincia de {province}, que es una de las provincias de tu país."; three="{city} está en {country}. Escribí {country}."; }
            else if (id == "explicit_color_balloon") { one="La pregunta pide el color del globo."; two="Buscá la palabra que dice cómo es el globo."; three="La oración dice ‘un globo rojo’. El color es rojo."; }
            else if (id == "explicit_when_doctor") { one="La pregunta pide saber cuándo tiene turno Matías."; two="Buscá la palabra que dice el día del turno."; three="La oración dice ‘el miércoles’. Matías tiene turno el miércoles."; }
            else if (id == "explicit_when_party") { one="La pregunta pide saber cuándo es la fiesta."; two="Buscá la palabra que dice el día de la fiesta."; three="La oración dice ‘el sábado’. La fiesta es el sábado."; }
            else if (id == "explicit_owner_ball") { one="La pregunta pide el nombre de la persona que tiene la pelota."; two="Buscá quién aparece junto a la pelota."; three="La oración dice ‘Tomás tiene una pelota’. La tiene Tomás."; }
            else if (id == "explicit_owner_book") { one="La pregunta pide el nombre de la persona que tiene el libro."; two="Buscá quién aparece junto al libro."; three="La oración dice ‘Lucía tiene un libro’. Lo tiene Lucía."; }
            else if (id == "explicit_location_cup") { one="La pregunta pide el lugar de la taza."; two="Buscá la palabra que dice dónde está."; three="La oración dice ‘en la mesa’. La taza está en la mesa."; }
            else if (id == "explicit_location_ball") { one="La pregunta pide el lugar de la pelota."; two="Buscá las palabras que dicen dónde está."; three="La oración dice ‘debajo de la silla’. La pelota está debajo de la silla."; }
            else if (id == "explicit_quantity_cats") { one="La pregunta pide una cantidad de gatos."; two="Buscá el número que acompaña a la palabra gatos."; three="La oración dice ‘Hay 3 gatos’. Escribí 3."; }
            else if (id == "explicit_quantity_pencils") { one="La pregunta pide una cantidad de lápices."; two="Buscá el número que acompaña a la palabra lápices."; three="La oración dice ‘Hay 4 lápices’. Escribí 4."; }
            else if (id == "explicit_action_nina") { one="La pregunta pide qué está dibujando Nina."; two="Buscá la palabra que aparece después de ‘dibuja’."; three="La oración dice ‘dibuja una flor’. Nina dibuja una flor."; }
            else if (id == "explicit_action_mateo") { one="La pregunta pide qué está comiendo Mateo."; two="Buscá la palabra que aparece después de ‘come’."; three="La oración dice ‘come una manzana’. Mateo come una manzana."; }
            else if (id == "explicit_object_dog") { one="La pregunta pide el lugar donde duerme el perro."; two="Buscá las palabras que aparecen después de ‘duerme’."; three="La oración dice ‘duerme en su cama’. El perro duerme en su cama."; }

            if (one == null || two == null || three == null) throw new InvalidOperationException("Faltan ayudas para la variante " + id + ".");
            return new List<MissionHelpStep> { Step(1, Resolve(one, mission)), Step(2, Resolve(two, mission)), Step(3, Resolve(three, mission)) };
        }

        public static MissionHelpStep AdaptiveHelpStep(Mission mission, int level, string previousAnswer)
        {
            var steps = HelpSteps(mission);
            if (level != 2 || mission.SkillId != "personal_location") return steps.Find(delegate(MissionHelpStep item) { return item.HelpLevel == level; });
            var context = mission.ContentContext;
            var answer = MissionText.Normalize(previousAnswer);
            var city = MissionText.Normalize(context.City);
            var province = MissionText.Normalize(context.Province);
            var country = MissionText.Normalize(context.Country);
            var asksCity = mission.VariantId == "location_city_ask_1" || mission.VariantId == "location_city_ask_2";
            var asksProvince = mission.VariantId == "location_province_ask_1" || mission.VariantId == "location_province_ask_2" || mission.VariantId == "location_city_to_province";
            string text;
            if (asksCity && answer == province) text = "{province} es la provincia. Te pregunta la ciudad: recordá cómo se llama la ciudad donde está tu casa.";
            else if (asksCity && answer == country) text = "{country} es el país. Te pregunta la ciudad: recordá cómo se llama la ciudad donde está tu casa.";
            else if (asksCity) text = "Recordá cómo se llama la ciudad donde está tu casa.";
            else if (asksProvince && answer == city) text = "{city} es la ciudad. Te pregunta la provincia donde está esa ciudad.";
            else if (asksProvince && answer == country) text = "{country} es el país. Te pregunta la provincia donde está tu ciudad.";
            else if (asksProvince) text = "Recordá cómo se llama la provincia donde está tu ciudad.";
            else if (answer == city) text = "{city} es la ciudad. Te pregunta el país donde vivís.";
            else if (answer == province) text = "{province} es una provincia. Pensá: vivís en la provincia de {province}, que es una de las provincias de tu país.";
            else text = "Pensá: vivís en la provincia de {province}, que es una de las provincias de tu país.";
            return Step(2, Resolve(text, mission));
        }

        private static string NicknameOrFallback(MissionContentContext context)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.Nickname)) return "Pensá en el nombre que figura como tu nombre.";
            return "Tu apodo es {nickname}. Acá te están preguntando tu nombre.";
        }

        private static string Resolve(string text, Mission mission)
        {
            var context = mission.ContentContext;
            text = ReplaceRequired(text, "{nickname}", context == null ? null : context.Nickname, mission.VariantId);
            text = ReplaceRequired(text, "{todayWeekday}", context == null ? null : context.TodayWeekday, mission.VariantId);
            text = ReplaceRequired(text, "{currentMonth}", context == null ? null : context.CurrentMonth, mission.VariantId);
            text = ReplaceRequired(text, "{0}", context == null ? null : context.Value, mission.VariantId);
            text = ReplaceRequired(text, "{a}", context == null ? null : context.FirstQuantity, mission.VariantId);
            text = ReplaceRequired(text, "{b}", context == null ? null : context.SecondQuantity, mission.VariantId);
            text = ReplaceRequired(text, "{city}", context == null ? null : context.City, mission.VariantId);
            text = ReplaceRequired(text, "{province}", context == null ? null : context.Province, mission.VariantId);
            text = ReplaceRequired(text, "{country}", context == null ? null : context.Country, mission.VariantId);
            return text;
        }

        private static string ReplaceRequired(string text, string token, string value, string variantId)
        {
            if (text.IndexOf(token, StringComparison.Ordinal) < 0) return text;
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Falta el valor dinámico " + token + " para la variante " + variantId + ".");
            return text.Replace(token, value);
        }

        private static MissionHelpStep Step(int level, string text) { return new MissionHelpStep { HelpLevel = level, Text = text }; }
    }
}
