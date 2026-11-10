using CoffeeNChillGateway.Dtos;
using CoffeeNChillGateway.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Controllers;

/// <summary>Menu items. Reading is public; changes need an Admin or Staff token.</summary>
[Route("api/menu")]
public class MenuController : GatewayControllerBase
{
    private readonly IHttpClientFactory _clients;

    public MenuController(IHttpClientFactory clients)
    {
        _clients = clients;
    }

    // Creates the named client for the Functions app (Microsoft, n.d.e).
    private HttpClient Functions => _clients.CreateClient("functions");

    /// <summary>Gets every menu item.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<MenuItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var response = await Functions.GetAsync("menu");
        return await Relay<List<FunctionMenuItem>, List<MenuItemResponse>>(response, MenuMapper.ToResponse);
    }

    /// <summary>Gets the menu items in one category.</summary>
    [HttpGet("category/{category}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<MenuItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var response = await Functions.GetAsync($"menu/category/{Uri.EscapeDataString(category)}");
        return await Relay<List<FunctionMenuItem>, List<MenuItemResponse>>(response, MenuMapper.ToResponse);
    }

    /// <summary>Creates a menu item (Admin or Staff).</summary>
    [HttpPost]
    // Only Admin and Staff roles may call this (Microsoft, n.d.f).
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(MenuItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateMenuItemRequest request)
    {
        var response = await Functions.PostAsync("menu", JsonBody(MenuMapper.ToFunctionCreate(request)));
        return await Relay<FunctionMenuItem, MenuItemResponse>(response, MenuMapper.ToResponse);
    }

    /// <summary>Updates a menu item (Admin or Staff).</summary>
    [HttpPut("{category}/{id}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(MenuItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(string category, string id, [FromBody] UpdateMenuItemRequest request)
    {
        var response = await Functions.PutAsync(
            $"menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}", JsonBody(request));
        return await Relay<FunctionMenuItem, MenuItemResponse>(response, MenuMapper.ToResponse);
    }

    /// <summary>Deletes a menu item (Admin or Staff).</summary>
    [HttpDelete("{category}/{id}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(string category, string id)
    {
        var response = await Functions.DeleteAsync(
            $"menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}");

        if (!response.IsSuccessStatusCode)
        {
            return FunctionsError(response, await response.Content.ReadAsStringAsync());
        }

        return NoContent();
    }
}
