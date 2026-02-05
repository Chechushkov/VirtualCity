using BCrypt.Net;

class Program
{
    static void Main()
    {
        string password = "adminpass";
        string hash = BCrypt.Net.BCrypt.HashPassword(password);
        Console.WriteLine("Password: " + password);
        Console.WriteLine("BCrypt Hash: " + hash);
        
        // Verify
        bool isValid = BCrypt.Net.BCrypt.Verify(password, hash);
        Console.WriteLine("Verification: " + isValid);
    }
}
