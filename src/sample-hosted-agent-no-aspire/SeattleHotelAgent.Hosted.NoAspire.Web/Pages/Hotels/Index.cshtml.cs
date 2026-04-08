using Microsoft.AspNetCore.Mvc.RazorPages;
using SeattleHotelAgent.Hosted.NoAspire.Models;

namespace SeattleHotelAgent.Hosted.NoAspire.Web.Pages.Hotels;

public class IndexModel : PageModel
{
    public List<Hotel> Hotels => HotelData.Hotels;
}
