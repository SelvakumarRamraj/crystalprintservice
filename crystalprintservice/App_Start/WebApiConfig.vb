Imports System.Web.Http
Public Module WebApiConfig
    Public Sub Register(config As HttpConfiguration)

        config.MapHttpAttributeRoutes()

        config.Routes.MapHttpRoute(
            name:="DefaultApi",
            routeTemplate:="api/{controller}/{action}/{id}",
            defaults:=New With {.id = RouteParameter.Optional}
        )

    End Sub
End Module
