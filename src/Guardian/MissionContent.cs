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

        // Prompts is the runtime source for every comprehension VariantId. Dynamic values are supplied by MissionSystem.
        private static readonly Dictionary<string, string> Prompts = new Dictionary<string, string> {
            { "identity_name_ask_1", "¿Cuál es tu nombre?" }, { "identity_name_ask_2", "¿Cómo te llamás?" }, { "identity_name_field", "Nombre:" }, { "identity_last_name_ask", "¿Cuál es tu apellido?" }, { "identity_last_name_field", "Apellido:" }, { "identity_name_last_name_ask", "¿Cuál es tu nombre y apellido?" }, { "identity_name_last_name_field", "Nombre y apellido:" }, { "identity_full_name_ask", "¿Cuál es tu nombre completo?" },
            { "age_ask_1", "¿Cuántos años tenés?" }, { "age_ask_2", "¿Qué edad tenés?" }, { "age_field", "Edad:" }, { "birth_year_ask", "¿En qué año naciste?" }, { "birth_year_field", "Año de nacimiento:" }, { "birthday_ask", "¿Cuándo es tu cumpleaños?" }, { "birth_date_ask", "¿Cuál es tu fecha de nacimiento?" },
            { "current_year_ask_1", "¿En qué año estamos?" }, { "current_year_ask_2", "¿Qué año es?" }, { "current_month_ask_1", "¿En qué mes estamos?" }, { "current_month_ask_2", "¿Qué mes es?" }, { "current_weekday", "¿Qué día de la semana es hoy?" }, { "current_day_of_month", "¿Qué día del mes es hoy?" }, { "current_full_date", "¿Qué fecha es hoy?" },
            { "tomorrow_weekday", "¿Qué día de la semana es mañana?" }, { "yesterday_weekday", "¿Qué día de la semana fue ayer?" }, { "next_month_ask_1", "¿Cuál es el mes que viene?" }, { "next_month_ask_2", "¿Qué mes viene después de este?" }, { "previous_month", "¿Cuál fue el mes pasado?" },
            { "days_in_week", "¿Cuántos días tiene una semana?" }, { "months_in_year", "¿Cuántos meses tiene un año?" }, { "weekday_after", "¿Qué día viene después del {0}?" }, { "weekday_before", "¿Qué día viene antes del {0}?" }, { "month_after", "¿Qué mes viene después de {0}?" }, { "month_before", "¿Qué mes viene antes de {0}?" },
            { "season_cold", "¿Cuál es la estación del año en la que hace mucho frío?" }, { "season_hot", "¿Cuál es la estación del año en la que hace mucho calor?" }, { "season_falling_leaves", "¿En qué estación se caen muchas hojas de los árboles?" }, { "season_flowers", "¿En qué estación suelen crecer muchas flores?" }, { "season_after", "¿Qué estación viene después del {0}?" },
            { "vocab_how_many", "⭐⭐⭐⭐ ¿Cuántas estrellas hay?" }, { "vocab_quantity", "Hay 3 lápices. ¿Cuál es la cantidad de lápices?" }, { "vocab_before", "Lunes, martes, miércoles. ¿Qué día está antes de miércoles?" }, { "vocab_after", "Enero, febrero, marzo. ¿Qué mes está después de febrero?" }, { "vocab_next", "Uno, dos, tres... ¿qué número es el siguiente?" }, { "vocab_previous", "Uno, dos, tres... ¿qué número es el anterior a tres?" }, { "vocab_first", "Rojo, azul, verde. ¿Cuál está primero?" }, { "vocab_last", "Rojo, azul, verde. ¿Cuál está último?" }
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
            if (id == "age_ask_1") { one="¿Qué cantidad de años tenés?"; two="Cuántos pregunta por una cantidad. ¿Qué número dice cuántos años tenés?"; three="Pensá en el número que decís cuando te preguntan tu edad."; }
            else if (id == "age_ask_2" || id == "age_field") { one="¿Cuántos años tenés?"; two="Edad quiere decir cuántos años tiene una persona."; three="Respondé con el número de años que tenés ahora."; }
            else if (id == "birth_year_ask" || id == "birth_year_field") { one="¿Cuál es el año en el que naciste?"; two="Pensá en la parte del año cuando escribís una fecha."; three="Escribí el año completo en que naciste."; }
            else if (id == "birth_date_ask") { one="¿Qué fecha dice el día, el mes y el año en que naciste?"; two="Fecha de nacimiento dice día, mes y año de nacimiento. Escribí los tres datos."; three="La respuesta tiene tres partes: día + mes + año en que naciste."; }
            else if (id == "birthday_ask") { one="¿Qué día y qué mes es tu cumpleaños?"; two="Te estoy preguntando el día y el mes de tu cumpleaños."; three="Pensá primero en el mes y después en qué día de ese mes es tu cumpleaños."; }
            else if (id == "season_falling_leaves") { one="¿En qué época del año suelen caerse las hojas de los árboles?"; two="Hay una estación en la que muchas hojas cambian de color y después caen."; three="Esa estación está entre verano e invierno."; }
            else if (id == "season_cold") { one="¿Cómo se llama la época del año en la que hace más frío?"; two="Pensá en la estación en la que usamos más abrigo."; three="No es verano: elegí la estación asociada al frío."; }
            else if (id == "season_hot") { one="¿Cómo se llama la época del año en la que hace más calor?"; two="Pensá en la estación de ropa liviana y días calurosos."; three="Elegí la estación asociada al calor."; }
            else if (id == "season_flowers") { one="¿En qué época del año aparecen muchas flores?"; two="Pensá en la estación que viene después del invierno."; three="Está entre invierno y verano."; }
            else if (id == "season_after") { one="¿Qué estación viene justo después de la mostrada?"; two="Pensá la secuencia: verano, otoño, invierno, primavera."; three="Ubicá la estación mostrada y elegí cuál sigue."; }
            else if (id == "vocab_how_many") { one="¿Qué cantidad de estrellas ves?"; two="Cuántas pregunta por una cantidad. Contalas."; three="Señalá una por una mientras contás y escribí el número final."; }
            else if (id == "vocab_quantity") { one="¿Cuántos lápices hay?"; two="Cantidad quiere decir cuántos hay."; three="Contá los lápices y escribí el número."; }
            else if (id == "vocab_before") { one="¿Qué día viene justo antes de miércoles?"; two="Mirá el orden: lunes → martes → miércoles."; three="Completá: lunes → ___ → miércoles."; }
            else if (id == "vocab_after") { one="¿Qué mes viene justo después de febrero?"; two="Mirá el orden: enero → febrero → marzo."; three="Completá: enero → febrero → ___."; }
            else if (id == "vocab_next") { one="¿Qué número viene después de tres?"; two="Siguiente quiere decir el que viene justo después."; three="Completá: 1, 2, 3, ___."; }
            else if (id == "vocab_previous") { one="¿Qué número viene justo antes de tres?"; two="Anterior quiere decir el que estaba antes."; three="Completá: 1, ___, 3."; }
            else if (id == "vocab_first") { one="¿Cuál aparece antes que los demás?"; two="Primero es el que está al comienzo."; three="Mirá el orden: rojo → azul → verde."; }
            else if (id == "vocab_last") { one="¿Cuál aparece al final?"; two="Último es el que está después de todos."; three="Mirá el orden: rojo → azul → verde."; }
            else if (id.StartsWith("current_year")) { one="¿Cuál es el año de ahora?"; two="Pensá en la parte del año cuando escribís la fecha de hoy."; three="Escribí el número de cuatro cifras del año actual."; }
            else if (id.StartsWith("current_month")) { one="¿Cuál es el mes de ahora?"; two="Pensá en enero, febrero, marzo… ¿cuál es el mes actual?"; three="Escribí el nombre del mes de ahora."; }
            else if (id == "current_full_date") { one="Respondé con el dato que pide esta consigna."; two="Pensá qué tipo de respuesta corresponde."; three="Elegí el dato correcto y escribilo completo."; }
            else if (id.Contains("month") || id.Contains("weekday")) { one="Pensá en el orden de los días o meses."; two="Ubicá el dato mostrado dentro de su secuencia."; three="Elegí el que corresponde antes o después."; }
            else if (id == "days_in_week" || id == "months_in_year") { one="¿Qué cantidad forman?"; two="Contalos en orden."; three="Escribí el número final."; }
            else if (mission.SkillId == "identity") { one="Pensá en el dato personal que pide la consigna."; two="Fijate si te pide nombre, apellido o los dos juntos."; three="Escribí completo el dato personal que te pide."; }
            if (one == null || two == null || three == null) throw new InvalidOperationException("Faltan ayudas para la variante " + id + ".");
            return new List<MissionHelpStep> { Step(1, one), Step(2, two), Step(3, three) };
        }

        private static MissionHelpStep Step(int level, string text) { return new MissionHelpStep { HelpLevel = level, Text = text }; }
    }
}
