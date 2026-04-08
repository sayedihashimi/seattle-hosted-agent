using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SeattleHotelAgent.Hosted.NoAspire.Models;

namespace SeattleHotelAgent.Hosted.NoAspire.Web.Pages.Hotels;

public class DetailModel : PageModel
{
    public Hotel? Hotel { get; set; }

    public IActionResult OnGet(string id)
    {
        Hotel = HotelData.Hotels.FirstOrDefault(h =>
            h.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        return Page();
    }
}
