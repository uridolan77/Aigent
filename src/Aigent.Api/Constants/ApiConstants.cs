namespace Aigent.Api.Constants
{
    /// <summary>
    /// Constants for the API
    /// </summary>
    public static class ApiConstants
    {
        /// <summary>
        /// Constants for API versions
        /// </summary>
        public static class Versions
        {
            /// <summary>
            /// Version 1.0
            /// </summary>
            public const string V1 = "1.0";
        }
        
        /// <summary>
        /// Constants for API routes
        /// </summary>
        public static class Routes
        {
            /// <summary>
            /// Base route for API version 1
            /// </summary>
            public const string ApiV1 = "api/v{version:apiVersion}";
            
            /// <summary>
            /// Agents route
            /// </summary>
            public const string Agents = "agents";
            
            /// <summary>
            /// Authentication route
            /// </summary>
            public const string Auth = "auth";
            
            /// <summary>
            /// Dashboard route
            /// </summary>
            public const string Dashboard = "dashboard";
            
            /// <summary>
            /// Workflows route
            /// </summary>
            public const string Workflows = "workflows";
        }
        
        /// <summary>
        /// Constants for authentication and authorization
        /// </summary>
        public static class Auth
        {
            /// <summary>
            /// Admin role
            /// </summary>
            public const string AdminRole = "Admin";
            
            /// <summary>
            /// User role
            /// </summary>
            public const string UserRole = "User";
            
            /// <summary>
            /// Admin only policy
            /// </summary>
            public const string AdminOnlyPolicy = "AdminOnly";
            
            /// <summary>
            /// Read only policy
            /// </summary>
            public const string ReadOnlyPolicy = "ReadOnly";
        }
        
        /// <summary>
        /// Constants for SignalR hubs
        /// </summary>
        public static class Hubs
        {
            /// <summary>
            /// Agent hub route
            /// </summary>
            public const string AgentHub = "/hubs/agent";
        }
    }
}
