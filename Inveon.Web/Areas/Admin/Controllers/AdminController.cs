using Microsoft.AspNetCore.Mvc;

namespace Inveon.Web.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class AdminController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Go()
		{
			return View();
		}

		public IActionResult AdminLogout()
		{
			return SignOut("Cookies", "oidc");
		}
    }
}
