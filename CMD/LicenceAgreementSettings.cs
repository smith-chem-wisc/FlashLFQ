using System;
using System.IO;

namespace CMD
{
    public class LicenceAgreementSettings
    {
        public static string PathToAssembly = AppDomain.CurrentDomain.BaseDirectory;
        public bool HasAcceptedThermoLicence { get; set; } = false;

        public LicenceAgreementSettings()
        {

        }

        public static LicenceAgreementSettings ReadLicenceSettings()
        {
            string path = Path.Combine(PathToAssembly, "LicenceAgreements.toml");
            LicenceAgreementSettings licenceAgreementSettings = new LicenceAgreementSettings();

            if (File.Exists(path))
            {
                try
                {
                    var table = Nett.Toml.ReadFile(path);
                    if (table.ContainsKey(nameof(HasAcceptedThermoLicence)))
                    {

                        licenceAgreementSettings.HasAcceptedThermoLicence = table.Get<bool>(nameof(HasAcceptedThermoLicence));
                    }
                    else
                    {
                        File.Delete(path);
                    }
                }
                catch (Exception e)
                {

                }
            }
            else
            {
                Nett.Toml.WriteFile(licenceAgreementSettings, path);
            }

            return licenceAgreementSettings;
        }

        public void AcceptLicenceAndWrite()
        {
            HasAcceptedThermoLicence = true;
            Nett.Toml.WriteFile(this, Path.Combine(PathToAssembly, "LicenceAgreements.toml"));
        }
    }
}
