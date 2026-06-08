using AppCore;
using Iros;
using Iros.Workshop;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: CatalogValidator <catalog.xml>");
            Environment.ExitCode = 1;
            return;
        }

        Console.WriteLine($"Validating: {args[0]}");

        try
        {
            Catalog catalog = Util.Deserialize<Catalog>(args[0]);
            List<InvalidLinkEntry> invalidLinkEntries = await ValidateDownloadLinksAsync(catalog);

            if (invalidLinkEntries.Count > 0) Console.WriteLine($"WARNING: Found {invalidLinkEntries.Count} invalid/unreachable download link(s).");

            Console.WriteLine("Validation completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Environment.ExitCode = 1;
        }
    }

    private static async Task<List<InvalidLinkEntry>> ValidateDownloadLinksAsync(Catalog catalog)
    {
        List<InvalidLinkEntry> invalidLinks = new List<InvalidLinkEntry>();

        foreach (Mod mod in catalog.Mods)
        {
            if (mod?.LatestVersion?.Links == null)
            {
                continue;
            }

            foreach (string rawLink in mod.LatestVersion.Links)
            {
                string modName = string.IsNullOrWhiteSpace(mod.Name) ? "<unknown mod>" : mod.Name;
                LinkValidationResult result = await DownloadLinkValidator.ValidateAsync(rawLink);

                if (!result.IsValid)
                {
                    string resolvedSuffix = string.IsNullOrWhiteSpace(result.ValidationTarget)
                        ? string.Empty
                        : $" [resolved: {result.ValidationTarget}]";

                    Console.WriteLine($"[INVALID] {mod.Name} ({mod.ID}): {rawLink} ({result.Error}){resolvedSuffix}");
                    invalidLinks.Add(new InvalidLinkEntry(modName, string.IsNullOrWhiteSpace(rawLink) ? "<empty>" : rawLink));
                }
            }
        }

        return invalidLinks;
    }

    private sealed record InvalidLinkEntry(string ModName, string DownloadLink);
}
