using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data;
using MyEccomerce.Hubs;
using MyEccomerce.Models;
using System.Security.Claims; // Importante para sa User Claims

namespace MyEccomerce.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;

        public CartController(ApplicationDbContext context, IHubContext<OrderHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // Helper Method: Kuhaon ang ID sa user nga naka-login
        private int GetCurrentUserId()
        {
            // 1. Kuhaon ang Email gikan sa Google login claims
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(email))
            {
                return 0; // Pasabot wala pa naka-login
            }

            // 2. Pangitaon sa database kinsa nga User ang naay ingon ani nga email
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            // 3. I-return ang tinuod nga ID gikan sa database
            return user != null ? user.UserId : 0;
        }

        [HttpPost]


        public async Task<IActionResult> AddToCart(int productId, int? variantId, int quantity)
        {
            int userId = GetCurrentUserId();

            if (userId == 0)
            {
                return Json(new { success = false, message = "Please login your Account." });
            }

            // Klarohon nato: Kung 0 ang gipasa gikan sa JavaScript, himoon natong null
            if (variantId == 0) { variantId = null; }

            // Pangitaon nato sa Cart kung naa na ba kani nga product PARA SA MAONG USER
            // Gi-check nato ang ProductId ug VariantId dungan
            var cartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId &&
                                          c.ProductId == productId &&
                                          c.VariantId == variantId);

            if (cartItem != null)
            {
                // Kung naa na sa cart, dugangan lang ang quantity
                cartItem.Quantity += quantity;
            }
            else
            {
                // Kung wala pa, mag-add og bag-o. Pwede ra ma-null ang VariantId diri karon.
                _context.Carts.Add(new Cart
                {
                    UserId = userId,
                    ProductId = productId,
                    VariantId = variantId, // Mahimo na kining null kung walay variation
                    Quantity = quantity,
                    DateAdded = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            // Gi-ihap pila ka buok nag-unang linya (rows) sa cart sa user
            int updatedCount = await _context.Carts.Where(c => c.UserId == userId).CountAsync();

            return Json(new
            {
                success = true,
                cartCount = updatedCount
            });
        }
        public async Task<IActionResult> Checkout(int? id, int? variantId, int qty = 1, string name = null, string price = null, string img = null)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            // Handle Unauthenticated (Buy Now Session)
            if (string.IsNullOrEmpty(userEmail))
            {
                if (!string.IsNullOrEmpty(name))
                {
                    HttpContext.Session.SetString("Pending_Name", name);
                    HttpContext.Session.SetString("Pending_Price", price);
                    HttpContext.Session.SetInt32("Pending_Qty", qty);
                    HttpContext.Session.SetString("Pending_Img", img);
                    HttpContext.Session.SetInt32("Pending_Id", id ?? 0);
                    if (variantId.HasValue) HttpContext.Session.SetInt32("Pending_VarId", variantId.Value);
                }
                return Challenge(new AuthenticationProperties { RedirectUri = "/Cart/Checkout" }, "Google");
            }

            // 1. OPTIMIZATION: .AsNoTracking() para mogaan ang pag-load sa User Profile sa memory
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null) return NotFound();

            var checkoutItems = new List<Cart>();

            // Check if "Buy Now" is active (from URL or Session)
            string finalName = name ?? HttpContext.Session.GetString("Pending_Name");

            if (!string.IsNullOrEmpty(finalName))
            {
                // BUY NOW LOGIC
                string finalPrice = price ?? HttpContext.Session.GetString("Pending_Price");
                int finalQty = qty != 0 ? qty : (HttpContext.Session.GetInt32("Pending_Qty") ?? 1);
                string finalImg = img ?? HttpContext.Session.GetString("Pending_Img");
                int finalId = id ?? (HttpContext.Session.GetInt32("Pending_Id") ?? 0);
                int? finalVarId = variantId ?? HttpContext.Session.GetInt32("Pending_VarId");
                if (finalVarId == 0) finalVarId = null;

                decimal p = 0;
                decimal.TryParse(finalPrice, out p);

                var item = new Cart
                {
                    ProductId = finalId,
                    VariantId = finalVarId,
                    Quantity = finalQty,
                    Product = new Product { Name = finalName, Price = p, ImageUrl = finalImg }
                };

                if (finalVarId.HasValue)
                {
                    // 2. OPTIMIZATION: .AsNoTracking() sab dinhi kay nag-basa ra ta sa variant details
                    item.Variant = await _context.ProductVariants
                        .AsNoTracking()
                        .FirstOrDefaultAsync(v => v.VariantId == finalVarId);
                }

                checkoutItems.Add(item);
                HttpContext.Session.Remove("Pending_Name"); // Clear session
            }
            else
            {
                // FROM DATABASE CART
                // 3. OPTIMIZATION: .AsNoTracking() lakip ang mga .Include() 
                // Dako kaayo nig tabang kung daghan silag gi-add sa cart para kausa ra i-ship sa Isla!
                checkoutItems = await _context.Carts
                    .AsNoTracking()
                    .Where(c => c.UserId == user.UserId)
                    .Include(c => c.Product)
                    .Include(c => c.Variant)
                    .ToListAsync();
            }

            return View("~/Pages/Public/Checkout.cshtml", new CheckoutViewModel { CartItems = checkoutItems, UserProfile = user });
        }
        [HttpGet]
        public IActionResult GetCartItems()
        {
            int userId = GetCurrentUserId();

            var items = _context.Carts
                .Include(c => c.Product)
                .Include(c => c.Variant)
                .Where(c => c.UserId == userId)
                .Select(c => new {
                    cartId = c.CartId,
                    productId = c.ProductId,
                    name = c.Product.Name,
                    price = c.Variant != null ? c.Variant.Price : c.Product.Price,

                    // FIX: Kani nga logic ang mag-match sa image sa saktong variation
                    imageUrl = (c.Variant != null && !string.IsNullOrEmpty(c.Variant.ImageUrl))
                                ? c.Variant.ImageUrl
                                : c.Product.ImageUrl,

                    quantity = c.Quantity,
                    variantName = c.Variant != null ? c.Variant.VariationName : ""
                }).ToList();

            return Json(items);
        }

        
        [HttpGet]
        public async Task<IActionResult> GetCartCount() // Usba gikan Task<int> ngadto sa Task<IActionResult>
        {
            int userId = GetCurrentUserId();
            int count = await _context.Carts.Where(c => c.UserId == userId).CountAsync();

            return Json(new { count = count }); // I-wrap sa JSON object
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int id) // 'id' ang gamita para sa JS match
        {
            var item = await _context.Carts.FindAsync(id);
            if (item != null)
            {
                _context.Carts.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        // Giusab ang 'int newQty' ngadto sa 'decimal newQty'
        public async Task<IActionResult> UpdateQuantity(int cartId, int newQty)
        {
            int userId = GetCurrentUserId();
            var item = await _context.Carts
                .Include(c => c.Product)
                .Include(c => c.Variant)
                .FirstOrDefaultAsync(c => c.CartId == cartId && c.UserId == userId);

            // 0.1 ang minimum para sa kilo
            if (item != null && newQty > 0)
            {
                item.Quantity = newQty; // Direkta na ma-save ang decimal sa DB
                await _context.SaveChangesAsync();

                var allItems = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Include(c => c.Product)
                    .Include(c => c.Variant)
                    .ToListAsync();

                // Ang math dinhi kay decimal * decimal na, so sakto na ang computation
                decimal merchSubtotal = allItems.Sum(x => (x.Variant != null ? x.Variant.Price : x.Product.Price) * x.Quantity);
                decimal shipping = 35;
                decimal grandTotal = merchSubtotal + shipping;

                return Json(new
                {
                    success = true,
                    // I-send balik ang subtotal nga naay 2 decimal places
                    newSubtotal = ((item.Variant != null ? item.Variant.Price : item.Product.Price) * item.Quantity).ToString("N2"),
                    merchSubtotal = merchSubtotal.ToString("N2"),
                    grandTotal = grandTotal.ToString("N2")
                });
            }
            return Json(new { success = false, message = "Invalid update." });
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string deliveryAddress)
        {
            // 1. Check Login
            int userId = GetCurrentUserId();
            if (userId == 0) return Json(new { success = false, message = "Palihog login una, boss." });

            // --- BACKEND ADDRESS VALIDATION GUARD ---
            // Sigurohon nga dili gyud blangko ang address nga gipasa gikan sa front-end
            if (string.IsNullOrEmpty(deliveryAddress) || deliveryAddress.Trim() == "")
            {
                return Json(new { success = false, message = "Ang delivery address gikinahanglan aron maka-checkout, boss." });
            }

            // 2. Get Cart Items
            var cartItems = await _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .Include(c => c.Variant)
                .ToListAsync();

            if (!cartItems.Any())
                return Json(new { success = false, message = "Empty imong cart, boss." });

            var userProfile = await _context.Users.FindAsync(userId);

            // 3. Create Order (Gigamit ang 'deliveryAddress' gikan sa front-end parameter)
            var newOrder = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = "Pending",
                // Note: Gi-maintain nako imong +35 nga delivery fee nga naa sa controller kaysa sa view markup
                TotalAmount = cartItems.Sum(item => (item.Variant != null ? item.Variant.Price : item.Product.Price) * item.Quantity) + 35,
                DeliveryAddress = deliveryAddress.Trim() // Kani ang bag-ong address nga gipili sa modal
            };

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync(); // I-save para naay OrderId

            // 4. Create OrderItems
            foreach (var item in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = newOrder.OrderId,
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    Quantity = item.Quantity,
                    Price = item.Variant != null ? item.Variant.Price : item.Product.Price
                };
                _context.OrderItems.Add(orderItem);
            }

            // 5. Clear Cart
            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            // 6. INSERT NOTIFICATION TO DATABASE (Direct SQL para walay Identity Error)
            string sql = @"INSERT INTO Notifications (UserId, Message, TargetUrl, UserProfilePicture, CreatedAt, IsRead) 
                   VALUES ({0}, {1}, {2}, {3}, {4}, {5})";

            string profilePic = !string.IsNullOrEmpty(userProfile?.ImageUrl)
                                ? userProfile.ImageUrl
                                : $"https://ui-avatars.com/api/?name={userProfile?.FirstName}&background=random";

            // "Admin" ang gibutang ingon nga identifier sa notification target
            await _context.Database.ExecuteSqlRawAsync(sql,
                "Admin",
                $"Bag-ong order gikan ni {userProfile?.FirstName ?? "usa ka Customer"}!",
                $"/Admin/Orders/Details/{newOrder.OrderId}",
                profilePic,
                DateTime.Now,
                false);

            // Gitangtang ang subra nga SaveChangesAsync() dinhi kay ExecuteSqlRawAsync mo-execute raman dretso sa DB

            // 7. SIGNALR: Notify Admins (Real-time alert)
            await _hubContext.Clients.Group("Admins").SendAsync("NewOrderAlert", new
            {
                orderId = newOrder.OrderId,
                customerName = userProfile?.FirstName ?? "A Customer",
                totalAmount = newOrder.TotalAmount.ToString("N2")
            });

            return Json(new { success = true, message = "Order placed successfully!", orderId = newOrder.OrderId });
        }


        public async Task<IActionResult> SaveDeliveryAddress(string address)
        {

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            int userId = (int)Convert.ToInt32(userIdString);
            if (userId == 0) return Json(new { success = false, message = "Palihog login una, boss." });

            if (string.IsNullOrEmpty(address))
            {
                return Json(new { success = false, message = "Dili pwede blangko ang address, boss." });
            }

            // Pangitaon ang profile sa user aron ma-update
            var userProfile = await _context.Users.FindAsync(userId);
            if (userProfile == null)
            {
                return Json(new { success = false, message = "Wala mapalgi ang imong Profile, boss." });
            }

            // I-save permanently sa database ang address string
            userProfile.Address = address.Trim();
            _context.Users.Update(userProfile);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Na-save na sa imong Profile sa database, boss!" });
        }


    }
}