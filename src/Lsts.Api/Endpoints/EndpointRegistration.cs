namespace Lsts.Api.Endpoints;

public static class EndpointRegistration
{
    public static WebApplication MapLstsEndpoints(this WebApplication app)
    {
        app.MapDbPingEndpoints();

        app.MapCustomersEndpoints();
        app.MapDevicesEndpoints();
        app.MapModelsEndpoints(); 

        app.MapPublicTicketEndpoints();

        app.MapTicketsEndpoints();
        app.MapTicketDashboardEndpoints();

        app.MapPartRequestsEndpoints();
        app.MapPartsEndpoints();

        app.MapDashboardEndpoints();

        return app;
    }
}