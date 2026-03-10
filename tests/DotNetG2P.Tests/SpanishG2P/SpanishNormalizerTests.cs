using DotNetG2P.Spanish.Normalization;

namespace DotNetG2P.Tests.SpanishG2P
{
    public class SpanishNormalizerTests
    {
        [Theory]
        [InlineData("  ¡Hola, MUNDO!  ", "hola mundo")]
        [InlineData("Sr. Gómez", "señor gómez")]
        [InlineData("Dra. Ruiz", "doctora ruiz")]
        [InlineData("Ud. y Uds.", "usted y ustedes")]
        [InlineData("５ gatos", "cinco gatos")]
        [InlineData("21%", "veintiuno por ciento")]
        [InlineData("$3 + 2", "tres dólares más dos")]
        [InlineData("pan & vino", "pan y vino")]
        [InlineData("3.14", "tres coma uno cuatro")]
        [InlineData("1.234", "mil doscientos treinta y cuatro")]
        [InlineData("1.234,56", "mil doscientos treinta y cuatro coma cinco seis")]
        [InlineData("12:30", "doce y treinta")]
        [InlineData("1:05", "una y cinco")]
        [InlineData("29:99", "veintinueve noventa y nueve")]
        [InlineData("12/10/2025", "doce de octubre de dos mil veinticinco")]
        [InlineData("1/1/2025", "primero de enero de dos mil veinticinco")]
        [InlineData("2025-10-12", "doce de octubre de dos mil veinticinco")]
        [InlineData("12.10.2025", "doce de octubre de dos mil veinticinco")]
        [InlineData("31/02/2025", "treinta y uno dos dos mil veinticinco")]
        [InlineData("12,5%", "doce coma cinco por ciento")]
        [InlineData("€3,50", "tres euros con cincuenta céntimos")]
        [InlineData("$1", "un dólar")]
        [InlineData("€1,01", "un euro con un céntimo")]
        [InlineData("3,5 km", "tres coma cinco kilómetros")]
        [InlineData("2 kg", "dos kilogramos")]
        [InlineData("1 h", "una hora")]
        [InlineData("21 h", "veintiuna horas")]
        [InlineData("1 m/s", "un metro por segundo")]
        [InlineData("2 km2", "dos kilómetros cuadrados")]
        [InlineData("3 °C", "tres grados celsius")]
        [InlineData("4 gb", "cuatro gigabytes")]
        [InlineData("EE. UU.", "estados unidos")]
        [InlineData("tel. 123", "teléfono ciento veintitrés")]
        [InlineData("Av. Núm. 5", "avenida número cinco")]
        [InlineData("Srta. López", "señorita lópez")]
        [InlineData("Ing. García", "ingeniero garcía")]
        [InlineData("p. ej. etc.", "por ejemplo etcétera")]
        [InlineData("Art. 5", "artículo cinco")]
        [InlineData("Cap. 2", "capítulo dos")]
        [InlineData("Dpto. 7", "departamento siete")]
        [InlineData("N.º 8", "número ocho")]
        [InlineData("Págs. 3-5", "páginas tres a cinco")]
        [InlineData("§ 4", "sección cuatro")]
        public void Normalize_ReturnsExpectedText(string input, string expected)
        {
            Assert.Equal(expected, SpanishNormalizer.Normalize(input));
        }
    }
}
