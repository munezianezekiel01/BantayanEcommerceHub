using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using MyEccomerce.Data;
using MyEccomerce.Hubs;
using MyEccomerce.Models;
using System.Net;

namespace MyEccomerce.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHost;
        private readonly IHubContext<StatusHub> _hubContext;
        public AdminController(ApplicationDbContext context, IWebHostEnvironment webHost, IHubContext<StatusHub> hubContext)
        {
            _context = context;
            _webHost = webHost;
            _hubContext = hubContext;
        }

        public IActionResult Dashboard()
        {
            return View("~/Pages/Admin/Dashboard.cshtml");
        }

 // Siguroha nga naa kini sa pinaka-itaas sa file

public IActionResult ProductList()
    {
        // 1. Kuhaa ang tanang produkto ug I-INCLUDE ang iyang mga ProductVariants (Eager Loading)
        var products = _context.Products
                               .Include(p => p.ProductVariants)
                               .ToList();

        // 2. I-return ang saktong path sa imong View ug i-pasa ang listahan sa mga produkto
        return View("~/Pages/Admin/ProductList.cshtml", products);
    }
    [HttpPost]
        [ValidateAntiForgeryToken]



        public async Task<IActionResult> UpdateProduct(Product submittedProduct)
        {
            // 1. Pangitaa ang orihinal nga product sa database lakip ang mga variants niini
            var dbProduct = await _context.Products
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == submittedProduct.ProductId);

            if (dbProduct == null)
            {
                return NotFound();
            }

            // 2. I-update ang nag-unang detalye sa produkto
            dbProduct.Name = submittedProduct.Name;
            dbProduct.Description = submittedProduct.Description;

            // 3. Check kung ang gipasa naay variants
            if (submittedProduct.ProductVariants != null && submittedProduct.ProductVariants.Any())
            {
                // I-update ang matag usa ka variant field
                foreach (var submittedVariant in submittedProduct.ProductVariants)
                {
                    var dbVariant = dbProduct.ProductVariants
                        .FirstOrDefault(v => v.VariantId == submittedVariant.VariantId);

                    if (dbVariant != null)
                    {
                        dbVariant.Price = submittedVariant.Price;
                        dbVariant.Stock = submittedVariant.Stock;
                        // Pwede pud nimo i-update ang SKU o ImageUrl diri kung imong gi-enable ang fields
                    }
                }

                // Optional logic: I-set ang main table Price/Stock base sa minimum sa iyang mga variants 
                // para dili ma-zeroed out ang main table registry columns
                dbProduct.Price = dbProduct.ProductVariants.Min(v => v.Price);
                dbProduct.Stock = dbProduct.ProductVariants.Sum(v => v.Stock);
            }
            else
            {
                // Kung standard product ra nga walay variant, ang main product price ug stock ang dawaton
                dbProduct.Price = submittedProduct.Price;
                dbProduct.Stock = submittedProduct.Stock;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index)); // Balik sa Product List page
        }


        public IActionResult AddProduct()
        {
            return View("~/Pages/Admin/AddProduct.cshtml");
        }


        [HttpGet]
        public async Task<IActionResult> AddVariation(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // I-pasa ang Product ID ug Product Name ngadto sa View gamit ang ViewBag
            ViewBag.ProductId = product.ProductId;
            ViewBag.ProductName = product.Name;

            return View();
        }

        // 2. POST: Admin/AddVariation
        // Mu-dawat sa data gikan sa porma ug mo-save sa database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVariation(ProductVariant variant)
        {
            if (ModelState.IsValid)
            {
                // Kung blangko ang SKU, mag-generate og automatic fallback
                if (string.IsNullOrEmpty(variant.SKU))
                {
                    variant.SKU = $"SKU-{variant.ProductId}-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";
                }

                _context.ProductVariants.Add(variant);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Bag-ong variation malampusong nadugang, brod!";

                // I-balik ang user ngadto sa listahan sa mga produkto
                return RedirectToAction(nameof(Index));
            }

            // Kung naay error sa validation, kuhaon og balik ang ngalan sa produkto para sa page header
            var product = await _context.Products.FindAsync(variant.ProductId);
            ViewBag.ProductId = variant.ProductId;
            ViewBag.ProductName = product?.Name ?? "Unknown Product";

            return View(variant);
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? ImageFile)
        {

            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    // Paghimo og unique filename para dili mag-overwrite
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);

                    // Path kung asa i-save ang image (wwwroot/images/products)
                    string uploadDir = Path.Combine(_webHost.WebRootPath, "images", "products");

                    // Siguruha nga nag-exist ang folder
                    if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                    string filePath = Path.Combine(uploadDir, fileName);

                    // I-copy ang file sa server folder
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }

                    // 2. I-save ang Path sa DB column nga ImageUrl
                    product.ImageUrl = "/images/products/" + fileName;
                }
                // Optional: I-set ang CreatedAt kung wala nimo gi-default sa Model
                product.CreatedAt = DateTime.Now;
                product.SoldCount = 0; // Default value para sa bag-ong product

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index"); // Balik sa listahan human ma-save
            }

            // Kung naay error sa form (validation error), balik sa form page
            return View("~/Pages/Admin/AddProduct.cshtml", product);
        }



        // Page para makita ang tanang orders
        public async Task<IActionResult> Orders()
        {

            var orders = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems) // I-include nato ang Variant para sa matag item
                    .ThenInclude(oi => oi.Variant)
                .OrderByDescending(o => o.OrderDate)
                .ToList();



            // 7. SIGNALR: Notify Admins (Real-time alert)

            return View("~/Pages/Admin/Orders.cshtml", orders);



        }

        [HttpGet("/Admin/Orders/Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            // 1. Pangitaon ang specific nga Order gamit ang ID gikan sa URL
            // Gi-apil (Include) nato ang User, Items, Product, ug Variant para kompleto ang display
            var order = await _context.Orders
                .Include(o => o.User) // Para sa Customer Name
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product) // Para sa Product Name/Image
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant) // Para sa Size/Color (kon naa)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            // 2. Kon wala makit-i ang Order ID (pananglitan gi-type og manual ang sayop nga ID)
            if (order == null)
            {
                return NotFound("Pasayloa, apan wala makit-i ang maong Order sa B-HUB system.");
            }

            // 3. I-return ang View nga nahimutang sa imong Pages folder
            // Siguroha nga ang file name kay 'OrderDetails.cshtml'
            return View("/Pages/Admin/OrderDetails.cshtml", order);
        }

        // Function para sa pag-update sa status (Action sa button)
        [HttpPost]
        
           public async Task<IActionResult> UpdateStatus(int orderId, string status)
            {
                // Mas maayo gamiton ang FindAsync kay async man atong method
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return Json(new { success = false });

                try
                {
                    order.Status = status;

                    // 1. Pag-set sa Note depende sa Status
                    string statusNote = status switch
                    {
                        "Confirmed" => "Gi-check na sa seller ang imong order ug gi-andam na.",
                        "Out for Delivery" => "Ang imong order gi-turn over na sa courier ug padulong na kanimo.",
                        "Completed" => "Salamat! Nadawat na ang order. Hinaot nagustohan nimo!",
                        "Cancelled" => "Ang imong order na-cancel. Kontaka ang admin kung naay pangutana.",
                        "Pending" => "Nagpaabot pa sa confirmation gikan sa seller.",
                        _ => $"Ang status sa imong order na-update sa {status}."
                    };

                    // 2. Create Notification Object
                    var newNotif = new Models.Notification
                    {
                        OrderId = orderId,
                        UserId = order.UserId.ToString(),
                        Message = $"Ang imong Order #{orderId} kay {status} na!",
                        Status = status,
                        IsRead = false,
                        CreatedAt = DateTime.Now,
                        UserProfilePicture = "https://ui-avatars.com/api/?name=Admin&background=0D6EFD&color=fff",
                        TargetUrl = $"/Orders/OrderDetails/{orderId}"
                    };

                    // 3. Create Order Log (Timeline)
                    _context.OrderLogs.Add(new OrderLog
                    {
                        OrderId = orderId,
                        Status = status,
                        Note = statusNote,
                        LogDate = DateTime.Now
                    });

                    _context.Notifications.Add(newNotif);

                    // I-save una sa database sa dili pa i-trigger ang SignalR
                    await _context.SaveChangesAsync();

                    string orderOwnerId = order.UserId.ToString();

                    // 4. SIGNALR REAL-TIME TRIGGER (Gi-apil na ang statusNote ug uban pang data)
                    await _hubContext.Clients.Group(orderOwnerId).SendAsync("NewUpdateAlert", new
                    {
                        orderId = orderId,
                        status = status,
                        note = statusNote, // Kinahanglan ni sa frontend para sa timeline
                        message = newNotif.Message, // Kinahanglan para sa notification popup
                        targetUrl = newNotif.TargetUrl
                    });




                    // 1. Siguroha nga makuha nimo ang UserId gikan sa order object


                    // 2. Ipadala ang alert ngadto sa iyang personal nga group (orderOwnerId) imbes nga "Updates"

                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
            public IActionResult Inventory()
            {
                // 1. Kuhaa ang tanang products gikan sa database
                var products = _context.Products.ToList();

                // 2. I-pasa ang 'products' list isip Model sa View
                return View("~/Pages/Admin/Inventory.cshtml", products);
            }



            [HttpPost]
            public async Task<IActionResult> Restock(int productId, int addQty)
            {
                var product = await _context.Products.FindAsync(productId);
                if (product != null)
                {
                    // 1. Update the Main Stock
                    product.Stock += addQty;

                    // 2. Add to Inventory Log
                    var log = new InventoryLog
                    {
                        ProductId = productId,
                        Quantity = addQty,
                        Type = "Restock",
                        Remarks = "Manual restock from admin"
                    };

                    _context.InventoryLogs.Add(log);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Index");
            }


            public async Task<IActionResult> MyClient()
            {
                var myclient = _context.Users.FirstOrDefault();

                return View("~/Pages/Admin/MyClient.cshtml", myclient);
            }



        }
    }
   
        // Siguroha nga naa kini sa ibabaw

       /* public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return Json(new { success = false });

            try
            {
                order.Status = status;

                string statusNote = status switch
                {
                    "Confirmed" => "Gi-check na sa seller ang imong order ug gi-andam na.",
                    "Out for Delivery" => "Ang imong order gi-turn over na sa courier ug padulong na kanimo.",
                    "Completed" => "Salamat! Nadawat na ang order. Hinaot nagustohan nimo!",
                    "Cancelled" => "Ang imong order na-cancel. Kontaka ang admin kung naay pangutana.",
                    "Pending" => "Nagpaabot pa sa confirmation gikan sa seller.",
                    _ => $"Ang status sa imong order na-update sa {status}."
                };

                var newNotif = new Models.Notification
                {
                    OrderId = orderId,
                    UserId = order.UserId.ToString(),
                    Message = $"Ang imong Order #{orderId} kay {status} na!",
                    Status = status,
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    UserProfilePicture = "https://ui-avatars.com/api/?name=Admin&background=0D6EFD&color=fff",
                    TargetUrl = $"/Orders/OrderDetails/{orderId}"
                };

                _context.OrderLogs.Add(new OrderLog
                {
                    OrderId = orderId,
                    Status = status,
                    Note = statusNote,
                    LogDate = DateTime.Now
                });

                _context.Notifications.Add(newNotif);
                await _context.SaveChangesAsync();

                string orderOwnerId = order.UserId.ToString();

                // -------------------------------------------------------------------------
                // A. SIGNALR (Para sa mga user nga KAKURANTING NAGA-AWAS / ABLI ANG BROWSER)
                // -------------------------------------------------------------------------
                await _hubContext.Clients.Group(orderOwnerId).SendAsync("NewUpdateAlert", new
                {
                    orderId = orderId,
                    status = status,
                    note = statusNote,
                    message = newNotif.Message,
                    targetUrl = newNotif.TargetUrl
                });

                // -------------------------------------------------------------------------
                // B. FIREBASE PUSH NOTIFICATION (Para sa mga user nga SIRADO ANG BROWSER)
                // -------------------------------------------------------------------------
                // 1. Kuhaa ang Device Token sa user gikan sa database (I-adjust kini depende sa imong Table name)
                var userTokenObj = await _context.UserTokens
                    .FirstOrDefaultAsync(t => t.UserId == order.UserId.ToString()); // Pananglitan og naay UserTokens table

                if (userTokenObj != null && !string.IsNullOrEmpty(userTokenObj.Token))
                {
                    // 2. Paghimo sa Firebase Message payload
                    var fcmMessage = new Message()
                    {
                        Token = userTokenObj.Token, // Ang bulawanong Device Token
                        Notification = new FirebaseAdmin.Messaging.Notification()
                        {
                            Title = "B-HUB Order Update",
                            Body = newNotif.Message // E.g., "Ang imong Order #123 kay Confirmed na!"
                        },
                        Data = new Dictionary<string, string>()
                {
                    { "orderId", orderId.ToString() },
                    { "targetUrl", newNotif.TargetUrl }
                }
                    };

                    // 3. I-send ang message sa Firebase Cloud Messaging server sa luyo
                    try
                    {
                        string response = await FirebaseMessaging.DefaultInstance.SendAsync(fcmMessage);
                        // Pwede nimo i-log ang response para sa debugging: Console.WriteLine("FCM Success: " + response);
                    }
                    catch (Exception fcmEx)
                    {
                        // Kung na-expire na ang token o napapas sa user, i-catch lang para dili ma-crash ang tibuok update transaction
                        Console.WriteLine("FCM Error: " + fcmEx.Message);
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


    }

}

        }*/
        

        

        