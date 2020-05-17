using Microsoft.AspNetCore.Mvc;

namespace Samples
{
    public class MyController : Controller
    {
        public IActionResult MyAction() => Content("Hello, from controllers!");
    }
}