using Iros;
using Iros.Workshop;

class Program {

    static void Main(string[] args)
    {
        Console.WriteLine($"Validating: {args[0]}");
        Util.Deserialize<Catalog>(args[0]);
        Console.WriteLine("Validation completed successfully.");
    }
}
