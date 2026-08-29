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

        public static string WritingFeedback(WritingDifference difference)
        {
            if (difference == WritingDifference.ExtraLetter) return "Parece que hay una letra de más. Leé cómo lo escribiste.";
            if (difference == WritingDifference.MissingLetter) return "Parece que falta una letra. Leé cómo lo escribiste.";
            if (difference == WritingDifference.TransposedLetters) return "Parece que dos letras están en otro orden. Leé cómo lo escribiste.";
            if (difference == WritingDifference.SubstitutedLetter) return "Parece que hay una letra que no va. Leé cómo lo escribiste.";
            return "Leé cómo lo escribiste.";
        }

        public static string WritingAnswerRevealed(string answer) { return "Se escribe: " + answer + ". Ahora escribilo vos correctamente."; }

        // Prompts is the runtime source for every comprehension VariantId. Dynamic {0} values are supplied by MissionSystem.
        private static readonly Dictionary<string, string> Prompts = new Dictionary<string, string> {
            { "identity_name_ask_1", "¿Cuál es tu nombre?" }, { "identity_name_ask_2", "¿Cómo te llamás?" }, { "identity_name_field", "Nombre:" }, { "identity_last_name_ask", "¿Cuál es tu apellido?" }, { "identity_last_name_field", "Apellido:" }, { "identity_name_last_name_ask", "¿Cuál es tu nombre y apellido?" }, { "identity_name_last_name_field", "Nombre y apellido:" }, { "identity_full_name_ask", "¿Cuál es tu nombre completo?" },
            { "age_ask_1", "¿Cuántos años tenés?" }, { "age_ask_2", "¿Qué edad tenés?" }, { "age_field", "Edad:" }, { "birth_year_ask", "¿En qué año naciste?" }, { "birth_year_field", "Año de nacimiento:" }, { "birthday_ask", "¿Cuándo es tu cumpleaños?" }, { "birth_date_ask", "¿Cuál es tu fecha de nacimiento?" },
            { "current_year_ask_1", "¿En qué año estamos?" }, { "current_year_ask_2", "¿Qué año es?" }, { "current_month_ask_1", "¿En qué mes estamos?" }, { "current_month_ask_2", "¿Qué mes es?" }, { "current_weekday", "¿Qué día de la semana es hoy?" }, { "current_day_of_month", "¿Qué día del mes es hoy?" }, { "current_full_date", "¿Qué fecha es hoy?" },
            { "tomorrow_weekday", "¿Qué día de la semana es mañana?" }, { "yesterday_weekday", "¿Qué día de la semana fue ayer?" }, { "next_month_ask_1", "¿Cuál es el mes que viene?" }, { "previous_month", "¿Cuál fue el mes pasado?" },
            { "days_in_week", "¿Cuántos días tiene una semana?" }, { "months_in_year", "¿Cuántos meses tiene un año?" }, { "weekday_after", "¿Qué día de la semana viene después del {0}?" }, { "weekday_before", "¿Qué día viene antes del {0}?" }, { "month_after", "¿Qué mes viene después de {0}?" }, { "month_before", "¿Qué mes viene antes de {0}?" },
            { "season_cold", "¿Cuál es la estación del año en la que hace mucho frío?" }, { "season_hot", "¿Cuál es la estación del año en la que hace mucho calor?" }, { "season_falling_leaves", "¿En qué estación se caen muchas hojas de los árboles?" }, { "season_flowers", "¿En qué estación suelen crecer muchas flores?" }, { "season_after", "¿Qué estación viene después del {0}?" },
            { "vocab_how_many", "⭐⭐⭐⭐ ¿Cuántas estrellas hay?" }, { "vocab_quantity", "Hay 3 lápices. ¿Cuántos lápices hay?" }, { "vocab_before", "Lunes, martes, miércoles. ¿Qué día está antes de miércoles?" }, { "vocab_after", "Enero, febrero, marzo. ¿Qué mes está después de febrero?" }, { "vocab_next", "Uno, dos, tres... ¿qué número es el siguiente?" }, { "vocab_previous", "Uno, dos, tres... ¿qué número es el anterior a tres?" }, { "vocab_first", "Rojo, azul, verde. ¿Cuál está primero?" }, { "vocab_last", "Rojo, azul, verde. ¿Cuál está último?" }
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

            if (one == null || two == null || three == null) throw new InvalidOperationException("Faltan ayudas para la variante " + id + ".");
            return new List<MissionHelpStep> { Step(1, Resolve(one, mission)), Step(2, Resolve(two, mission)), Step(3, Resolve(three, mission)) };
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
