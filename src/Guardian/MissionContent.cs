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

        public static List<MissionHelpStep> HelpSteps(Mission mission)
        {
            string one = "Respondé con el dato que pide esta consigna.";
            string two = "Pensá qué tipo de respuesta corresponde.";
            string three = "Elegí el dato correcto y escribilo completo.";
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
            else if (id.Contains("month") || id.Contains("weekday")) { one="Pensá en el orden de los días o meses."; two="Ubicá el dato mostrado dentro de su secuencia."; three="Elegí el que corresponde antes o después."; }
            else if (id == "days_in_week" || id == "months_in_year") { one="¿Qué cantidad forman?"; two="Contalos en orden."; three="Escribí el número final."; }
            else if (mission.SkillId == "identity") { one="Pensá en el dato personal que pide la consigna."; two="Fijate si te pide nombre, apellido o los dos juntos."; three="Escribí completo el dato personal que te pide."; }
            return new List<MissionHelpStep> { Step(1, one), Step(2, two), Step(3, three) };
        }

        private static MissionHelpStep Step(int level, string text) { return new MissionHelpStep { HelpLevel = level, Text = text }; }
    }
}
