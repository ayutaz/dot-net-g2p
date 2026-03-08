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
        [InlineData("12:30", "doce y treinta")]
        [InlineData("EE. UU.", "estados unidos")]
        [InlineData("tel. 123", "teléfono ciento veintitrés")]
        [InlineData("Av. Núm. 5", "avenida número cinco")]
        public void Normalize_ReturnsExpectedText(string input, string expected)
        {
            Assert.Equal(expected, SpanishNormalizer.Normalize(input));
        }
    }
}
