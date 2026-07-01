Imports System.Web.Mvc
Imports System.Web.Routing
Imports System.Web.Http
Public Class MvcApplication
    Inherits System.Web.HttpApplication

    Protected Sub Application_Start()

        GlobalConfiguration.Configure(AddressOf WebApiConfig.Register)
        'RouteConfig.RegisterRoutes(RouteTable.Routes)
        ' AreaRegistration.RegisterAllAreas()
        'RouteConfig.RegisterRoutes(RouteTable.Routes)
    End Sub
End Class
