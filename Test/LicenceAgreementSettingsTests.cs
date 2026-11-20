using System;
using System.IO;
using NUnit.Framework;
using Util;

namespace Test
{
    [TestFixture]
    public class LicenceAgreementSettingsTests
    {
        private string _originalPathToAssembly;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _originalPathToAssembly = LicenceAgreementSettings.PathToAssembly;
            _tempDir = Path.Combine(Path.GetTempPath(), "FlashLfqLicenceTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            LicenceAgreementSettings.PathToAssembly = _tempDir;
        }

        [TearDown]
        public void TearDown()
        {
            LicenceAgreementSettings.PathToAssembly = _originalPathToAssembly;
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(_tempDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            Directory.Delete(_tempDir, true);
        }

        [Test]
        public void ReadLicenceSettings_MissingFile_DoesNotWrite()
        {
            if (!OperatingSystem.IsWindows())
            {
                // read-only install directory, as on a shared Linux module install
                File.SetUnixFileMode(_tempDir, UnixFileMode.UserRead | UnixFileMode.UserExecute);
            }

            LicenceAgreementSettings settings = null;
            Assert.That(() => settings = LicenceAgreementSettings.ReadLicenceSettings(), Throws.Nothing);
            Assert.That(settings.HasAcceptedThermoLicence, Is.False);
            Assert.That(File.Exists(Path.Combine(_tempDir, "LicenceAgreements.toml")), Is.False);
        }

        [Test]
        public void ReadLicenceSettings_AcceptedFile_IsRead()
        {
            File.WriteAllText(Path.Combine(_tempDir, "LicenceAgreements.toml"), "HasAcceptedThermoLicence = true\n");

            Assert.That(LicenceAgreementSettings.ReadLicenceSettings().HasAcceptedThermoLicence, Is.True);
        }

        [Test]
        public void AcceptLicenceAndWrite_RoundTrips()
        {
            new LicenceAgreementSettings().AcceptLicenceAndWrite();

            Assert.That(LicenceAgreementSettings.ReadLicenceSettings().HasAcceptedThermoLicence, Is.True);
        }
    }
}
