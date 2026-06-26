using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services
{
    public class VehiclePaintPaletteService
    {
        public List<VehiclePaintColor> GetPaletteForMake(string make)
        {
            make = (make ?? "").Trim().ToLowerInvariant();

            return make switch
            {
                "pontiac" => GmColors(),
                "chevrolet" => GmColors(),
                "gmc" => GmColors(),
                "cadillac" => GmColors(),
                "buick" => GmColors(),

                "ford" => FordColors(),
                "lincoln" => FordColors(),

                "dodge" => MoparColors(),
                "ram" => MoparColors(),
                "chrysler" => MoparColors(),
                "jeep" => MoparColors(),

                "toyota" => ToyotaColors(),
                "lexus" => ToyotaColors(),

                _ => GenericColors()
            };
        }

        private List<VehiclePaintColor> GmColors()
        {
            return new()
            {
                new() { Name="Summit White", Code="GAZ", Hex="#F5F5F3" },
                new() { Name="Black", Code="41U", Hex="#111111" },
                new() { Name="Victory Red", Code="74U", Hex="#C4122E" },
                new() { Name="Torch Red", Code="WA9075", Hex="#D1111C" },
                new() { Name="Dark Cherry", Code="WA505Q", Hex="#541A21" },
                new() { Name="Silver Ice Metallic", Code="GAN", Hex="#B5B7BA" },
                new() { Name="Cyber Gray", Code="WA637R", Hex="#60646B" },
                new() { Name="Stealth Gray", Code="G7Q", Hex="#54585C" },
                new() { Name="Sport Red Metallic", Code="WA9260", Hex="#8B1820" },
                new() { Name="Blue Metallic", Code="WA928L", Hex="#21416B" }
            };
        }

        private List<VehiclePaintColor> FordColors()
        {
            return new()
            {
                new() { Name="Oxford White", Code="YZ", Hex="#F4F4F1" },
                new() { Name="Shadow Black", Code="G1", Hex="#101010" },
                new() { Name="Race Red", Code="PQ", Hex="#C11726" },
                new() { Name="Rapid Red", Code="D4", Hex="#7E0F1B" },
                new() { Name="Atlas Blue", Code="B3", Hex="#1E4E90" },
                new() { Name="Velocity Blue", Code="E7", Hex="#145ED0" },
                new() { Name="Carbonized Gray", Code="M7", Hex="#55575A" },
                new() { Name="Iconic Silver", Code="JS", Hex="#B7B9BC" }
            };
        }

        private List<VehiclePaintColor> MoparColors()
        {
            return new()
            {
                new() { Name="Pitch Black", Code="PX8", Hex="#0E0E0E" },
                new() { Name="TorRed", Code="PR3", Hex="#D01222" },
                new() { Name="Go Mango", Code="PVP", Hex="#F36A21" },
                new() { Name="Plum Crazy", Code="PHG", Hex="#652D90" },
                new() { Name="Destroyer Gray", Code="PDN", Hex="#7A7B7D" },
                new() { Name="F8 Green", Code="PFQ", Hex="#1D4E3D" },
                new() { Name="White Knuckle", Code="PW7", Hex="#F6F6F6" },
                new() { Name="B5 Blue", Code="PQD", Hex="#205ECF" }
            };
        }

        private List<VehiclePaintColor> ToyotaColors()
        {
            return new()
            {
                new() { Name="Super White", Code="040", Hex="#F5F5F2" },
                new() { Name="Midnight Black", Code="218", Hex="#121212" },
                new() { Name="Celestial Silver", Code="1J9", Hex="#A9ADB2" },
                new() { Name="Magnetic Gray", Code="1G3", Hex="#5C6165" },
                new() { Name="Blueprint", Code="8X8", Hex="#203B72" },
                new() { Name="Supersonic Red", Code="3U5", Hex="#A91721" },
                new() { Name="Army Green", Code="6V7", Hex="#4A5644" }
            };
        }

        private List<VehiclePaintColor> GenericColors()
        {
            return new()
            {
                new() { Name="White", Code="GEN1", Hex="#F2F2F2" },
                new() { Name="Black", Code="GEN2", Hex="#111111" },
                new() { Name="Silver", Code="GEN3", Hex="#B8B8B8" },
                new() { Name="Gray", Code="GEN4", Hex="#707070" },
                new() { Name="Red", Code="GEN5", Hex="#C61A25" },
                new() { Name="Blue", Code="GEN6", Hex="#2458C4" },
                new() { Name="Green", Code="GEN7", Hex="#2C6D47" }
            };
        }
    }
}