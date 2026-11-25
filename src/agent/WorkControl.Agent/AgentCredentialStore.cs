using System.Security.Cryptography;
using System.Text;

namespace WorkControl.Agent;

public sealed class AgentCredentialStore
{
    private static string SecretPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WorkControl", "agent-credential.bin");

    public void Save(string secret)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SecretPath)!);
        var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(secret), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(SecretPath, encrypted);
    }

    public string? Load()
    {
        if (!File.Exists(SecretPath)) return null;
        try
        {
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(SecretPath), null, DataProtectionScope.CurrentUser));
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public void Delete()
    {
        if (File.Exists(SecretPath)) File.Delete(SecretPath);
    }
}
