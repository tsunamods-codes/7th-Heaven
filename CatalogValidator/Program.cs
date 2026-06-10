using AppCore;
using Iros;
using Iros.Workshop;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: CatalogValidator <catalog.xml> [--validate-links]");
            Environment.ExitCode = 1;
            return;
        }

        string catalogPath = args[0];
        bool validateLinks = args.Contains("--validate-links");

        Console.WriteLine($"Validating: {catalogPath}");

        try
        {
            Catalog catalog = Util.Deserialize<Catalog>(catalogPath);

            if (validateLinks)
            {
                (List<ModLinkStatus> warnings, List<ModLinkStatus> errors) = await ValidateDownloadLinksAsync(catalog);

                if (errors.Count > 0)
                {
                    WriteErrorsData(Console.Out, errors);
                }

                if (warnings.Count > 0)
                {
                    WriteWarningsData(Console.Out, warnings);
                }

                if (warnings.Count > 0 || errors.Count > 0)
                {
                    GenerateResultFiles(catalogPath, warnings, errors);
                }
            }

            Console.WriteLine("Validation completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Environment.ExitCode = 1;
        }
    }

    private static async Task<(List<ModLinkStatus>, List<ModLinkStatus>)> ValidateDownloadLinksAsync(Catalog catalog)
    {
        List<ModLinkStatus> warnings = new List<ModLinkStatus>();
        List<ModLinkStatus> errors = new List<ModLinkStatus>();

        foreach (Mod mod in catalog.Mods)
        {
            if (mod?.LatestVersion?.Links == null || mod.LatestVersion.Links.Count == 0)
            {
                continue;
            }

            string modName = string.IsNullOrWhiteSpace(mod.Name) ? "<unknown mod>" : mod.Name;
            List<LinkStatus> linkStatuses = new List<LinkStatus>();
            bool hasWorkingLink = false;

            foreach (string rawLink in mod.LatestVersion.Links)
            {
                LinkValidationResult result = await DownloadLinkValidator.ValidateAsync(rawLink);
                linkStatuses.Add(new LinkStatus(
                    result.IsValid,
                    rawLink,
                    result.ValidationTarget,
                    result.Error
                ));

                if (result.IsValid)
                {
                    hasWorkingLink = true;
                }
            }

            // Determine the status for this mod based on aggregated results
            if (!hasWorkingLink && linkStatuses.Count > 0)
            {
                // All links are broken
                errors.Add(new ModLinkStatus(modName, mod.ID.ToString(), linkStatuses));
            }
            else if (hasWorkingLink && linkStatuses.Any(l => !l.IsValid))
            {
                // At least one works, but some are broken
                warnings.Add(new ModLinkStatus(modName, mod.ID.ToString(), linkStatuses));
            }
        }

        return (warnings, errors);
    }

    private sealed record LinkStatus(bool IsValid, string Url, string ResolvedTarget, string Error);
    private sealed record ModLinkStatus(string ModName, string ModId, List<LinkStatus> Links);

    private static void GenerateResultFiles(string catalogPath, List<ModLinkStatus> warnings, List<ModLinkStatus> errors)
    {
        string catalogDirectory = Path.GetDirectoryName(catalogPath) ?? Directory.GetCurrentDirectory();
        string catalogFileName = Path.GetFileNameWithoutExtension(catalogPath);

        if (errors.Count > 0)
        {
            string errorsFile = Path.Combine(catalogDirectory, $"{catalogFileName}_errors.txt");
            using (var writer = new StreamWriter(errorsFile, false, System.Text.Encoding.UTF8))
            {
                WriteErrorsData(writer, errors);
            }
        }

        if (warnings.Count > 0)
        {
            string warningsFile = Path.Combine(catalogDirectory, $"{catalogFileName}_warnings.txt");
            using (var writer = new StreamWriter(warningsFile, false, System.Text.Encoding.UTF8))
            {
                WriteWarningsData(writer, warnings);
            }
        }
    }

    private static void WriteWarningsData(TextWriter writer, List<ModLinkStatus> warnings)
    {
        writer.WriteLine($"⚠️ Found {warnings.Count} mod(s) with at least one working link, but some didn't resolve correctly:\n");
        foreach (var warning in warnings)
        {
            writer.WriteLine($"» {warning.ModName} ({warning.ModId})");
            foreach (var link in warning.Links)
            {
                if (!link.IsValid)
                {
                    writer.WriteLine($"  - {link.Url} ({link.Error})");
                }
            }
        }
    }

    private static void WriteErrorsData(TextWriter writer, List<ModLinkStatus> errors)
    {
        writer.WriteLine($"❌ Found {errors.Count} mod(s) with no working links:\n");
        foreach (var error in errors)
        {
            writer.WriteLine($"» {error.ModName} ({error.ModId})");
            foreach (var link in error.Links)
            {
                writer.WriteLine($"  - {link.Url} ({link.Error})");
            }
        }
    }
}
