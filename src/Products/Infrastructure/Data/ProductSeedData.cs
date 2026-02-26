using Core.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Domain.Products.Entities;
using Infrastructure.Data;


namespace Data;

public static class ProductSeedData
{
    // Fixed GUIDs so Stock seed can reference them
    static readonly Guid[] Ids =
    [
        Guid.Parse("10000000-0001-0000-0000-000000000001"),
        Guid.Parse("10000000-0001-0000-0000-000000000002"),
        Guid.Parse("10000000-0001-0000-0000-000000000003"),
        Guid.Parse("10000000-0001-0000-0000-000000000004"),
        Guid.Parse("10000000-0001-0000-0000-000000000005"),
        Guid.Parse("10000000-0001-0000-0000-000000000006"),
        Guid.Parse("10000000-0002-0000-0000-000000000001"),
        Guid.Parse("10000000-0002-0000-0000-000000000002"),
        Guid.Parse("10000000-0002-0000-0000-000000000003"),
        Guid.Parse("10000000-0002-0000-0000-000000000004"),
        Guid.Parse("10000000-0002-0000-0000-000000000005"),
        Guid.Parse("10000000-0003-0000-0000-000000000001"),
        Guid.Parse("10000000-0003-0000-0000-000000000002"),
        Guid.Parse("10000000-0003-0000-0000-000000000003"),
        Guid.Parse("10000000-0003-0000-0000-000000000004"),
        Guid.Parse("10000000-0003-0000-0000-000000000005"),
        Guid.Parse("10000000-0004-0000-0000-000000000001"),
        Guid.Parse("10000000-0004-0000-0000-000000000002"),
        Guid.Parse("10000000-0004-0000-0000-000000000003"),
        Guid.Parse("10000000-0004-0000-0000-000000000004"),
        Guid.Parse("10000000-0004-0000-0000-000000000005"),
        Guid.Parse("10000000-0005-0000-0000-000000000001"),
        Guid.Parse("10000000-0005-0000-0000-000000000002"),
        Guid.Parse("10000000-0005-0000-0000-000000000003"),
        Guid.Parse("10000000-0005-0000-0000-000000000004"),
        Guid.Parse("10000000-0005-0000-0000-000000000005"),
        Guid.Parse("10000000-0006-0000-0000-000000000001"),
        Guid.Parse("10000000-0006-0000-0000-000000000002"),
        Guid.Parse("10000000-0006-0000-0000-000000000003"),
        Guid.Parse("10000000-0007-0000-0000-000000000001"),
        Guid.Parse("10000000-0007-0000-0000-000000000002"),
        Guid.Parse("10000000-0007-0000-0000-000000000003"),
        Guid.Parse("10000000-0007-0000-0000-000000000004"),
        Guid.Parse("10000000-0007-0000-0000-000000000005"),
        Guid.Parse("10000000-0008-0000-0000-000000000001"),
        Guid.Parse("10000000-0008-0000-0000-000000000002"),
        Guid.Parse("10000000-0008-0000-0000-000000000003"),
        Guid.Parse("10000000-0008-0000-0000-000000000004"),
        Guid.Parse("10000000-0008-0000-0000-000000000005"),
        Guid.Parse("10000000-0008-0000-0000-000000000006"),
        Guid.Parse("10000000-0007-0000-0000-000000000006"),
        Guid.Parse("10000000-0006-0000-0000-000000000004"),
    ];

    // Category IDs
    const string CatPeripherals = "a1507da2-eff7-4d99-878d-cc29a3e9eb57";
    const string CatAudio       = "0a3e52e1-7e6a-4f9d-9249-72d4f6a3cb0e";
    const string CatAccessories = "5f914e7b-2e2e-4374-a1f4-3ddc64f8c245";
    const string CatMonitors    = "b3c1d2e3-f4a5-6b7c-8d9e-0f1a2b3c4d5e";
    const string CatStorage     = "c4d2e3f4-a5b6-7c8d-9e0f-1a2b3c4d5e6f";
    const string CatNetworking  = "d5e3f4a5-b6c7-8d9e-0f1a-2b3c4d5e6f70";
    const string CatGaming      = "e6f4a5b6-c7d8-9e0f-1a2b-3c4d5e6f7081";
    const string CatLaptops     = "f7a5b6c7-d8e9-0f1a-2b3c-4d5e6f708192";

    // Uses a specific Unsplash photo ID; crop param lets Enrich vary the composition for additional images
    private static string Img(string unsplashId, string crop = "center") =>
        $"https://images.unsplash.com/photo-{unsplashId}?w=400&h=400&fit=crop&crop={crop}&auto=format&q=80";

    // Representative Unsplash photo IDs per category (used for additional images)
    private static readonly Dictionary<string, string> CategoryImages = new()
    {
        [CatPeripherals] = "1587829741301-dc798b83add3",  // mechanical keyboard
        [CatAudio]       = "1505740420928-5e560c06d30e",  // Sony-style headphones
        [CatAccessories] = "1558171813-d13d0c5e7b49",    // USB accessories
        [CatMonitors]    = "1527443224154-c4a3942d3acf",  // computer monitor
        [CatStorage]     = "1597754742025-5bb5de4bfb6b",  // SSD drive
        [CatNetworking]  = "1562408590-e32931084e23",     // router
        [CatGaming]      = "1593640408182-31c70c8268f5",  // gaming setup
        [CatLaptops]     = "1496181133206-80ce9b88a853",  // MacBook
    };

    private static void Enrich(Product p, int idx)
    {
        var catImg = CategoryImages.GetValueOrDefault(p.CategoryId, "1496181133206-80ce9b88a853");
        p.AdditionalImages = [Img(catImg, "left"), Img(catImg, "right"), Img(catImg, "entropy")];
        p.Rating = 3.5 + (idx % 15) * 0.1;
        p.ReviewCount = 50 + idx * 37 % 500;
        p.IsBestSeller = idx % 4 == 0;
        p.IsNewRelease = idx % 5 == 0;
        p.IsDeal = idx % 6 == 0;
        p.DealPrice = p.IsDeal ? Math.Round(p.Price * 0.8, 2) : null;
        p.CreatedAt = DateTime.UtcNow.AddDays(-idx * 3);
        p.AboutHtml = $"<ul><li>Premium build quality with advanced materials</li><li>Designed for professionals and enthusiasts</li><li>Industry-leading performance in its category</li><li>{p.Description}</li></ul>";
    }

    private static List<ProductSpec> PeripheralSpecs() =>
    [
        new() { Label = "Connectivity", Value = "Bluetooth / USB-C" },
        new() { Label = "Battery Life", Value = "70 hours" },
        new() { Label = "Weight", Value = "141g" },
        new() { Label = "DPI", Value = "200–8000" },
        new() { Label = "Compatibility", Value = "Windows, macOS, Linux" },
    ];

    private static List<ProductSpec> AudioSpecs() =>
    [
        new() { Label = "Driver Size", Value = "40mm" },
        new() { Label = "Frequency Response", Value = "20Hz–20kHz" },
        new() { Label = "Battery Life", Value = "30 hours" },
        new() { Label = "Noise Cancelling", Value = "Active (ANC)" },
        new() { Label = "Connectivity", Value = "Bluetooth 5.3 / 3.5mm" },
    ];

    private static List<ProductSpec> MonitorSpecs() =>
    [
        new() { Label = "Panel Type", Value = "IPS" },
        new() { Label = "Resolution", Value = "3840 x 2160 (4K)" },
        new() { Label = "Refresh Rate", Value = "60Hz" },
        new() { Label = "Response Time", Value = "5ms (GtG)" },
        new() { Label = "Ports", Value = "HDMI 2.0, DP 1.4, USB-C" },
    ];

    private static List<ProductSpec> StorageSpecs() =>
    [
        new() { Label = "Interface", Value = "PCIe 4.0 NVMe" },
        new() { Label = "Sequential Read", Value = "7,450 MB/s" },
        new() { Label = "Sequential Write", Value = "6,900 MB/s" },
        new() { Label = "Form Factor", Value = "M.2 2280" },
        new() { Label = "Endurance", Value = "1,200 TBW" },
    ];

    private static List<ProductSpec> LaptopSpecs() =>
    [
        new() { Label = "Processor", Value = "Intel Core i7 / Apple M3" },
        new() { Label = "RAM", Value = "16 GB" },
        new() { Label = "Storage", Value = "512 GB SSD" },
        new() { Label = "Display", Value = "14–16 inch Retina / OLED" },
        new() { Label = "Battery", Value = "Up to 18 hours" },
    ];

    private static List<ProductSpec> DefaultSpecs() =>
    [
        new() { Label = "Brand", Value = "Various" },
        new() { Label = "Warranty", Value = "1 Year" },
        new() { Label = "Connectivity", Value = "USB / Wireless" },
    ];

    private static List<ProductFaq> DefaultFaqs(string name) =>
    [
        new() { Question = $"Is the {name} compatible with Mac?", Answer = "Yes, it works with macOS, Windows, and most Linux distributions." },
        new() { Question = "What is the warranty period?", Answer = "This product comes with a 1-year manufacturer warranty." },
        new() { Question = "Does it come with a carrying case?", Answer = "Please check the product description for included accessories. Most premium products include protective packaging." },
    ];

    private static List<ProductSpec> SpecsForCategory(string catId) => catId switch
    {
        CatPeripherals => PeripheralSpecs(),
        CatAudio => AudioSpecs(),
        CatMonitors => MonitorSpecs(),
        CatStorage => StorageSpecs(),
        CatLaptops => LaptopSpecs(),
        _ => DefaultSpecs(),
    };

    public static async Task InitializeAsync(ProductDbContext context, IProductMessagingService messaging)
    {
        var categoryMap = new Dictionary<string, Category>
        {
            [CatPeripherals] = new() { Id = CatPeripherals, Name = "Peripherals" },
            [CatAudio]       = new() { Id = CatAudio,       Name = "Audio" },
            [CatAccessories] = new() { Id = CatAccessories, Name = "Accessories" },
            [CatMonitors]    = new() { Id = CatMonitors,    Name = "Monitors" },
            [CatStorage]     = new() { Id = CatStorage,     Name = "Storage" },
            [CatNetworking]  = new() { Id = CatNetworking,  Name = "Networking" },
            [CatGaming]      = new() { Id = CatGaming,      Name = "Gaming" },
            [CatLaptops]     = new() { Id = CatLaptops,     Name = "Laptops" },
        };

        var productsExist = await context.Products.AsNoTracking().FirstOrDefaultAsync() != null;
        if (!productsExist)
        {

            var i = 0;
            var products = new List<Product>
            {
                // ── Peripherals ──
                new() { Id = Ids[i++], Name = "Logitech MX Master 3S",           Price = 99.99,   Description = "Advanced wireless mouse with ultra-fast scrolling and 8K DPI sensor",              ImageUrl = Img("1527864550417-7fd91fc51a46"), CategoryId = CatPeripherals },
                new() { Id = Ids[i++], Name = "Logitech MX Keys Mini",            Price = 79.99,   Description = "Minimalist wireless illuminated keyboard with smart backlighting",                 ImageUrl = Img("1587829741301-dc798b83add3"), CategoryId = CatPeripherals },
                new() { Id = Ids[i++], Name = "Razer DeathAdder V3",              Price = 89.99,   Description = "Ultra-lightweight ergonomic esports mouse with 30K optical sensor",                ImageUrl = Img("1527864550417-7fd91fc51a46", "entropy"), CategoryId = CatPeripherals },
                new() { Id = Ids[i++], Name = "Corsair K70 RGB Pro",              Price = 159.99,  Description = "Mechanical gaming keyboard with Cherry MX switches and RGB",                       ImageUrl = Img("1587829741301-dc798b83add3", "entropy"), CategoryId = CatPeripherals },
                new() { Id = Ids[i++], Name = "SteelSeries Aerox 3 Wireless",    Price = 59.99,   Description = "Ultra-lightweight wireless gaming mouse at 68g",                                    ImageUrl = Img("1527864550417-7fd91fc51a46", "left"),    CategoryId = CatPeripherals },
                new() { Id = Ids[i++], Name = "Keychron Q1 Pro",                  Price = 199.00,  Description = "QMK/VIA wireless custom mechanical keyboard with gasket mount",                    ImageUrl = Img("1587829741301-dc798b83add3", "left"),    CategoryId = CatPeripherals },

                // ── Audio ──
                new() { Id = Ids[i++], Name = "SteelSeries Arctis Nova 7",       Price = 179.99,  Description = "Multi-platform wireless gaming headset with 38-hour battery",                      ImageUrl = Img("1599669851046-5fce00fd3f3d"),             CategoryId = CatAudio },
                new() { Id = Ids[i++], Name = "Sony WH-1000XM5",                 Price = 348.00,  Description = "Industry-leading noise cancelling wireless headphones",                             ImageUrl = Img("1505740420928-5e560c06d30e"),             CategoryId = CatAudio },
                new() { Id = Ids[i++], Name = "Apple AirPods Pro 2",              Price = 249.00,  Description = "Active noise cancelling with adaptive audio and USB-C",                            ImageUrl = Img("1600585154526-990dced4db0d"),             CategoryId = CatAudio },
                new() { Id = Ids[i++], Name = "Bose QuietComfort Ultra",          Price = 429.00,  Description = "Immersive spatial audio with world-class noise cancellation",                      ImageUrl = Img("1484704849700-f032a568e944"),             CategoryId = CatAudio },
                new() { Id = Ids[i++], Name = "HyperX Cloud III",                 Price = 99.99,   Description = "Gaming headset with angled 53mm drivers and DTS spatial audio",                    ImageUrl = Img("1546435770-a3e426bf472b"),                CategoryId = CatAudio },

                // ── Accessories ──
                new() { Id = Ids[i++], Name = "Anker USB-C Hub 7-in-1",           Price = 35.99,   Description = "USB-C hub with 4K HDMI, USB 3.0 ports and SD card reader",                        ImageUrl = Img("1558171813-d13d0c5e7b49"),                CategoryId = CatAccessories },
                new() { Id = Ids[i++], Name = "Apple Magic Trackpad",             Price = 129.00,  Description = "Wireless Multi-Touch trackpad with Force Touch technology",                        ImageUrl = Img("1611532736597-de2d4265fba3"),             CategoryId = CatAccessories },
                new() { Id = Ids[i++], Name = "Elgato Stream Deck MK.2",          Price = 149.99,  Description = "Studio controller with 15 customizable LCD keys",                                  ImageUrl = Img("1593640408182-31c70c8268f5", "left"),    CategoryId = CatAccessories },
                new() { Id = Ids[i++], Name = "CalDigit TS4 Thunderbolt Dock",    Price = 399.99,  Description = "18-port Thunderbolt 4 docking station for pro workflows",                          ImageUrl = Img("1558171813-d13d0c5e7b49", "entropy"),    CategoryId = CatAccessories },
                new() { Id = Ids[i++], Name = "Logitech Spotlight Presenter",     Price = 129.99,  Description = "Advanced digital pointer with smart highlighting and timer",                       ImageUrl = Img("1558171813-d13d0c5e7b49", "right"),     CategoryId = CatAccessories },

                // ── Monitors ──
                new() { Id = Ids[i++], Name = "Dell UltraSharp U2723QE",          Price = 619.99,  Description = "27-inch 4K USB-C Hub Monitor with IPS Black technology",                           ImageUrl = Img("1527443224154-c4a3942d3acf"),             CategoryId = CatMonitors },
                new() { Id = Ids[i++], Name = "LG 27GP850-B UltraGear",           Price = 449.99,  Description = "27-inch QHD Nano IPS 165Hz gaming monitor with G-Sync",                            ImageUrl = Img("1593640408182-31c70c8268f5"),             CategoryId = CatMonitors },
                new() { Id = Ids[i++], Name = "Samsung Odyssey G7 32\"",           Price = 699.99,  Description = "32-inch WQHD 240Hz curved gaming monitor with 1ms response",                      ImageUrl = Img("1527443224154-c4a3942d3acf", "entropy"), CategoryId = CatMonitors },
                new() { Id = Ids[i++], Name = "ASUS ProArt PA278QV",              Price = 329.00,  Description = "27-inch WQHD professional monitor with 100% sRGB and Rec.709",                    ImageUrl = Img("1527443224154-c4a3942d3acf", "left"),    CategoryId = CatMonitors },
                new() { Id = Ids[i++], Name = "BenQ PD2725U DesignVue",           Price = 899.00,  Description = "27-inch 4K Thunderbolt 3 designer monitor with HDR10",                             ImageUrl = Img("1527443224154-c4a3942d3acf", "right"),   CategoryId = CatMonitors },

                // ── Storage ──
                new() { Id = Ids[i++], Name = "Samsung 990 Pro 2TB NVMe",         Price = 189.99,  Description = "PCIe 4.0 NVMe M.2 SSD with up to 7450 MB/s sequential read",                      ImageUrl = Img("1597754742025-5bb5de4bfb6b"),             CategoryId = CatStorage },
                new() { Id = Ids[i++], Name = "WD Black SN850X 1TB",              Price = 89.99,   Description = "High-performance NVMe SSD designed for gaming with heatsink",                      ImageUrl = Img("1597754742025-5bb5de4bfb6b", "entropy"), CategoryId = CatStorage },
                new() { Id = Ids[i++], Name = "Samsung T7 Shield 2TB",            Price = 159.99,  Description = "Portable SSD with IP65 water and dust resistance rating",                          ImageUrl = Img("1597754742025-5bb5de4bfb6b", "left"),    CategoryId = CatStorage },
                new() { Id = Ids[i++], Name = "Crucial MX500 1TB SATA",           Price = 69.99,   Description = "Reliable 2.5-inch SATA SSD with 560 MB/s sequential read",                        ImageUrl = Img("1597754742025-5bb5de4bfb6b", "right"),   CategoryId = CatStorage },
                new() { Id = Ids[i++], Name = "Seagate Expansion 4TB",            Price = 94.99,   Description = "Portable external hard drive compatible with PC and Mac",                          ImageUrl = Img("1597754742025-5bb5de4bfb6b", "top"),     CategoryId = CatStorage },

                // ── Networking ──
                new() { Id = Ids[i++], Name = "TP-Link Deco XE75 3-Pack",         Price = 299.99,  Description = "Wi-Fi 6E tri-band whole home mesh system up to 5500 sqft",                        ImageUrl = Img("1562408590-e32931084e23"),                CategoryId = CatNetworking },
                new() { Id = Ids[i++], Name = "ASUS RT-AX86U Pro",                Price = 249.99,  Description = "Dual-band Wi-Fi 6 gaming router with AiMesh support",                              ImageUrl = Img("1562408590-e32931084e23", "entropy"),     CategoryId = CatNetworking },
                new() { Id = Ids[i++], Name = "Netgear Nighthawk AX12",           Price = 399.99,  Description = "12-stream Wi-Fi 6 router with up to 6 Gbps combined speed",                       ImageUrl = Img("1562408590-e32931084e23", "left"),       CategoryId = CatNetworking },

                // ── Gaming ──
                new() { Id = Ids[i++], Name = "Xbox Wireless Controller",         Price = 59.99,   Description = "Wireless controller with textured grip for Xbox and PC",                           ImageUrl = Img("1605899435591-d1c3e2f78db3"),             CategoryId = CatGaming },
                new() { Id = Ids[i++], Name = "PS5 DualSense Controller",         Price = 69.99,   Description = "Wireless controller with haptic feedback and adaptive triggers",                    ImageUrl = Img("1606144042614-1b29b50dd2ee"),             CategoryId = CatGaming },
                new() { Id = Ids[i++], Name = "Razer Wolverine V2 Chroma",        Price = 149.99,  Description = "Wired gaming controller with Razer Mecha-Tactile buttons",                        ImageUrl = Img("1605899435591-d1c3e2f78db3", "entropy"), CategoryId = CatGaming },
                new() { Id = Ids[i++], Name = "SteelSeries QcK Heavy XXL",        Price = 34.99,   Description = "Extra-thick gaming mouse pad 900x400mm non-slip rubber base",                     ImageUrl = Img("1626958097536-e3c98ce5c4c0"),             CategoryId = CatGaming },
                new() { Id = Ids[i++], Name = "Elgato HD60 X Capture Card",       Price = 199.99,  Description = "External capture card with 4K60 HDR10 passthrough",                                ImageUrl = Img("1593640408182-31c70c8268f5", "right"),   CategoryId = CatGaming },

                // ── Laptops ──
                new() { Id = Ids[i++], Name = "Apple MacBook Air M3 15\"",         Price = 1299.00, Description = "15-inch Liquid Retina display with M3 chip and 18h battery",                       ImageUrl = Img("1496181133206-80ce9b88a853"),             CategoryId = CatLaptops },
                new() { Id = Ids[i++], Name = "Dell XPS 15",                       Price = 1499.99, Description = "15.6-inch FHD+ InfinityEdge display with Intel Core i7-13700H",                   ImageUrl = Img("1593642632559-0c6d3fc62b89"),             CategoryId = CatLaptops },
                new() { Id = Ids[i++], Name = "Lenovo ThinkPad X1 Carbon Gen 11",  Price = 1649.00, Description = "14-inch ultralight business laptop with 13th Gen Intel vPro",                     ImageUrl = Img("1484788984921-03950022c9ef"),             CategoryId = CatLaptops },
                new() { Id = Ids[i++], Name = "ASUS ROG Zephyrus G14",            Price = 1599.99, Description = "14-inch QHD 165Hz gaming laptop with RTX 4060 and Ryzen 9",                       ImageUrl = Img("1593642702749-b7d2a804fbf7"),             CategoryId = CatLaptops },
                new() { Id = Ids[i++], Name = "HP Spectre x360 16",               Price = 1799.99, Description = "16-inch 2-in-1 OLED touch laptop with Intel Core i7",                              ImageUrl = Img("1593642632559-0c6d3fc62b89", "entropy"), CategoryId = CatLaptops },
                new() { Id = Ids[i++], Name = "Acer Swift Go 14",                 Price = 849.99,  Description = "14-inch OLED display with Intel Core Ultra and 16GB RAM",                          ImageUrl = Img("1593642632559-0c6d3fc62b89", "left"),    CategoryId = CatLaptops },

                // Extra to round out
                new() { Id = Ids[i++], Name = "Elgato Key Light Air",              Price = 129.99,  Description = "Professional LED panel for streaming and video calls",                             ImageUrl = Img("1567879021502-1ffe5b4e2fba"),             CategoryId = CatGaming },
                new() { Id = Ids[i++], Name = "TP-Link Archer AX55",               Price = 129.99,  Description = "AX3000 dual-band Wi-Fi 6 router with HomeCare security",                          ImageUrl = Img("1562408590-e32931084e23", "right"),     CategoryId = CatNetworking },
            };

            // Enrich all products with specs, FAQs, ratings, and flags
            for (var idx = 0; idx < products.Count; idx++)
            {
                var p = products[idx];
                Enrich(p, idx);
                var cat = categoryMap[p.CategoryId];
                p.Category = new Category { Id = cat.Id, Name = cat.Name };
                p.Specs = SpecsForCategory(p.CategoryId);
                p.Faqs = DefaultFaqs(p.Name);
            }

            // Save each product individually to avoid EF Core owned-entity
            // tracking conflicts (multiple Category instances share the same Id).
            foreach (var product in products)
            {
                context.Products.Add(product);
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
            }

            foreach (var product in products)
                await messaging.SendProductUpdate(product);
        }
        else
        {
            // Backfill: patch any existing products that are missing the embedded Category or have old image URLs
            var allProducts = await context.Products.ToListAsync();
            var toUpdate = allProducts
                .Where(p => p.Category == null && categoryMap.ContainsKey(p.CategoryId))
                .ToList();

            if (toUpdate.Count > 0)
            {
                foreach (var p in toUpdate)
                {
                    var cat = categoryMap[p.CategoryId];
                    p.Category = new Category { Id = cat.Id, Name = cat.Name };
                }

                // Fix image URLs that still point at picsum or loremflickr
                foreach (var p in toUpdate.Where(p => p.ImageUrl.Contains("picsum.photos") || p.ImageUrl.Contains("loremflickr")))
                {
                    var catImg = CategoryImages.GetValueOrDefault(p.CategoryId, "1496181133206-80ce9b88a853");
                    p.ImageUrl = Img(catImg);
                    p.AdditionalImages = [Img(catImg, "left"), Img(catImg, "right"), Img(catImg, "entropy")];
                }

                await context.SaveChangesAsync();
            }

            // Always republish all products so the basket Redis product cache is restored
            // after a volume wipe or fresh container start.
            foreach (var p in allProducts)
                await messaging.SendProductUpdate(p);
        }
    }
}