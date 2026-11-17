using Lab_BE1_2_25DH190025.Models;
using Lab_BE1_2_25DH190025.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Data.Entity.Validation;

namespace Lab_BE1_2_25DH190025.Controllers
{
    public class OrderController : Controller
    {
        private MyStoreEntities db = new MyStoreEntities();
        // GET: Order
        public ActionResult Index()
        {
            return View();
        }
        private List<CartItem> GetCartItems()
        {
            var cartService = Session["Cart"] as Cart;
            if (cartService != null)
            {
                return cartService.Items.ToList();
            }
            return Session["Cart"] as List<CartItem>;
        }

        // GET: Order/Checkout
        [Authorize]
        public ActionResult Checkout()
        {
            //Kiểm tra giỏ hàng trong session,
            //nếu giỏ hàng rỗng hoặc không có sản phẩm thì chuyển hướng về trang chủ
            var cart = GetCartItems();
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            //Xác thực người dùng đã đăng nhập chưa, nếu chưa thì chuyển hướng tới trang đăng nhập
            var user = db.Users.SingleOrDefault(u => u.Username == User.Identity.Name);
            if (user == null) { return RedirectToAction("Login", "Account"); }

            //Lấy thông tin khách hàng từ CSDL, nếu chưa có thì chuyển hướng tới trang đăng nhập
            //nếu có rồi thì lấy địa chỉ của khách hàng và gán vào ShippingAddress của trang CheckoutVM
            var customer = db.Customers.SingleOrDefault(c => c.Username == user.Username);
            if (customer == null) { return RedirectToAction("Login", "Account"); }

            var model = new CheckoutVM
            { //Tạo dữ liệu hiện thị cho CheckoutVM
                CartItems = cart, //Lấy danh sách sản phẩm trong giỏ hàng
                TotalAmount = cart.Sum(item => item.TotalPrice), //Tổng giá trị của các mặt hàng trong giỏ
                OrderDate = DateTime.Now, //Mặc định lấy bằng thời điểm đặt hàng
                ShippingAddress = customer.CustomerAddress, // Lấy địa chỉ mặc định từ bảng Customer
                CustomerID = customer.CustomerID, // Lấy mã khách hàng từ bảng Customer
                Username = customer.Username // Lấy tên đăng nhập từ bảng Customer
            };
            return View(model);
        }
        // POST: Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Checkout(CheckoutVM model)
        {
            var cart = GetCartItems();
            //Nếu giỏ hàng rỗng sẽ điều hướng tới trang Home
            if (cart == null || !cart.Any()) { return RedirectToAction("Index", "Home"); }

            if (ModelState.IsValid)
            {
                //Nếu người dùng chưa đăng nhập sẽ điều hướng tới trang Login
                var user = db.Users.SingleOrDefault(u => u.Username == User.Identity.Name);
                if (user == null) { return RedirectToAction("Login", "Account"); }

                //Nếu khách hàng không khớp với tên đăng nhập, sẽ điều hướng tới trang Login
                var customer = db.Customers.SingleOrDefault(c => c.Username == user.Username);
                if (customer == null) { return RedirectToAction("Login", "Account"); }

                //Nếu người dùng chọn thanh toán bằng Paypal, sẽ điều hướng tới trang PaymentWithPaypal
                if (model.PaymentMethod == "Paypal")
                {
                    model.CartItems = cart;
                    model.TotalAmount = cart.Sum(item => item.TotalPrice);

                    // Lưu thông tin Checkout để sử dụng sau khi PayPal thanh toán
                    Session["PendingCheckout"] = model;

                    return RedirectToAction("PaymentWithPaypal", "PayPal", model);
                }

                // Thiết lập PaymentStatus dựa trên PaymentMethod
                string paymentStatus = "Chưa thanh toán";
                switch (model.PaymentMethod)
                {
                    case "Tiền mặt": paymentStatus = "Thanh toán tiền mặt"; break;
                    case "Paypal": paymentStatus = "Thanh toán paypal"; break;
                    case "Mua trước trả sau": paymentStatus = "Trả góp"; break;
                    default: paymentStatus = "Chưa thanh toán"; break;
                }

                //Tạo đơn hàng và chi tiết đơn hàng liên quan
                var order = new Order
                {
                    CustomerID = customer.CustomerID,
                    OrderDate = model.OrderDate == default(DateTime) ? DateTime.Now : model.OrderDate,
                    TotalAmount = cart.Sum(item => item.TotalPrice),
                    PaymentStatus = paymentStatus,
                    PaymentMethod = model.PaymentMethod,
                    ShippingMethod = model.ShippingMethod,
                    ShippingAddress = model.ShippingAddress,

                    // Thêm dòng này
                    AddressDelivery = model.ShippingAddress,

                    OrderDetails = cart.Select(item => new OrderDetail
                    {
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    }).ToList()
                };

                // Lưu đơn hàng vào CSDL
                db.Orders.Add(order);
                db.SaveChanges();

                // Xóa giỏ hàng sau khi đặt hàng thành công
                Session["Cart"] = null;
                // Điều hướng tới trang Xác nhận đơn hàng
                return RedirectToAction("OrderSuccess", new { id = order.OrderID });
            }
            model.CartItems = cart;
            return View(model);
        }
        [Authorize]
        public ActionResult CreateOrder()
        {
            var model = Session["PendingCheckout"] as CheckoutVM;
            var cart = GetCartItems();

            if (model == null || cart == null || !cart.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            var user = db.Users.SingleOrDefault(
                u => u.Username == User.Identity.Name
            );

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = db.Customers.SingleOrDefault(
                c => c.Username == user.Username
            );

            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = new Order
            {
                CustomerID = customer.CustomerID,
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(item => item.TotalPrice),
                PaymentStatus = "Đã thanh toán",
                PaymentMethod = "Paypal",
                ShippingMethod = model.ShippingMethod,
                ShippingAddress = model.ShippingAddress,

                OrderDetails = cart.Select(item => new OrderDetail
                {
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                }).ToList()
            };

            db.Orders.Add(order);
            db.SaveChanges();

            // Xóa thông tin giỏ hàng và Checkout
            Session["Cart"] = null;
            Session["PendingCheckout"] = null;

            return RedirectToAction("OrderSuccess",
                new { id = order.OrderID });
        }
        public ActionResult OrderSuccess(int id)
        {
            var order = db.Orders
                .Include("OrderDetails.Product")
                .SingleOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }
    }
}