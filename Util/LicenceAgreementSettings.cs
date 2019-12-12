using System;
using System.IO;

namespace Util
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

            try
            {
                Nett.Toml.WriteFile(this, Path.Combine(PathToAssembly, "LicenceAgreements.toml"));
            }
            catch (Exception e)
            {
                // for some reason the file could not be written - maybe because we don't have permission to write files to this directory
                // we can continue on with the data analysis but they will probably need to re-accept the licence every time since we can't store the result

                throw new Exception("Your agreement of the licence could not be stored. Make sure FlashLFQ has permission to write to its folder. " +
                    "Analysis will continue, but you'll need to accept the licence again next time.");
            }
        }
    }
}
