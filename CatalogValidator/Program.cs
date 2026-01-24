using Iros;
using Iros.Workshop;

class Program {

    static void Main(string[] args)
    {
        Console.WriteLine($"Validating: {args[0]}");
        try {
            Util.Deserialize<Catalog>(args[0]);
            Console.WriteLine("Validation completed successfully.");
        }
        catch (Exception ex) {
            Console.WriteLine(ex.ToString());
            Environment.ExitCode = 1;
        }
    }
}
