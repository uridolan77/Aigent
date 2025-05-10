using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Aigent.Core;
using Aigent.Api.Models;

namespace Aigent.Client
{
    /// <summary>
    /// Client for the Aigent API
    /// </summary>
    public class AigentClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;
        private HubConnection _hubConnection;
        private string _token;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the AigentClient class
        /// </summary>
        /// <param name="baseUrl">Base URL of the API</param>
        /// <param name="httpClient">HTTP client to use</param>
        public AigentClient(string baseUrl, HttpClient httpClient = null)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = httpClient ?? new HttpClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Authenticates with the API
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>Authentication result</returns>
        public async Task<AuthResult> AuthenticateAsync(string username, string password)
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password
            };
            
            var response = await PostAsync<AuthResult>("/api/v1/auth/login", request);
            
            if (response.Success && response.Data != null)
            {
                _token = response.Data.Token;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            }
            
            return response.Data;
        }

        /// <summary>
        /// Gets the current user
        /// </summary>
        /// <returns>User information</returns>
        public async Task<UserDto> GetCurrentUserAsync()
        {
            var response = await GetAsync<UserDto>("/api/v1/auth/me");
            return response.Data;
        }

        /// <summary>
        /// Gets all agents
        /// </summary>
        /// <param name="name">Filter by name</param>
        /// <param name="type">Filter by type</param>
        /// <param name="status">Filter by status</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="sortBy">Sort by field</param>
        /// <param name="sortDirection">Sort direction</param>
        /// <returns>List of agents</returns>
        public async Task<List<AgentDto>> GetAgentsAsync(
            string name = null,
            string type = null,
            string status = null,
            int page = 1,
            int pageSize = 10,
            string sortBy = null,
            string sortDirection = "asc")
        {
            var queryParams = new Dictionary<string, string>();
            
            if (!string.IsNullOrEmpty(name)) queryParams["name"] = name;
            if (!string.IsNullOrEmpty(type)) queryParams["type"] = type;
            if (!string.IsNullOrEmpty(status)) queryParams["status"] = status;
            queryParams["page"] = page.ToString();
            queryParams["pageSize"] = pageSize.ToString();
            if (!string.IsNullOrEmpty(sortBy)) queryParams["sortBy"] = sortBy;
            queryParams["sortDirection"] = sortDirection;
            
            var response = await GetAsync<List<AgentDto>>("/api/v1/agents", queryParams);
            return response.Data;
        }

        /// <summary>
        /// Gets an agent by ID
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>The agent</returns>
        public async Task<AgentDto> GetAgentAsync(string agentId)
        {
            var response = await GetAsync<AgentDto>($"/api/v1/agents/{agentId}");
            return response.Data;
        }

        /// <summary>
        /// Creates a new agent
        /// </summary>
        /// <param name="request">Agent creation request</param>
        /// <returns>The created agent</returns>
        public async Task<AgentDto> CreateAgentAsync(CreateAgentRequest request)
        {
            var response = await PostAsync<AgentDto>("/api/v1/agents", request);
            return response.Data;
        }

        /// <summary>
        /// Deletes an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Whether the agent was deleted</returns>
        public async Task<bool> DeleteAgentAsync(string agentId)
        {
            var response = await DeleteAsync<bool>($"/api/v1/agents/{agentId}");
            return response.Data;
        }

        /// <summary>
        /// Performs an action with an agent
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="input">Input for the agent</param>
        /// <param name="parameters">Additional parameters for the action</param>
        /// <returns>Result of the action</returns>
        public async Task<AgentActionResponse> PerformActionAsync(
            string agentId, 
            string input, 
            Dictionary<string, object> parameters = null)
        {
            var request = new AgentActionRequest
            {
                Input = input,
                Parameters = parameters ?? new Dictionary<string, object>()
            };
            
            var response = await PostAsync<AgentActionResponse>($"/api/v1/agents/{agentId}/actions", request);
            return response.Data;
        }

        /// <summary>
        /// Executes a workflow
        /// </summary>
        /// <param name="request">Workflow creation request</param>
        /// <returns>Result of the workflow execution</returns>
        public async Task<WorkflowResultDto> ExecuteWorkflowAsync(CreateWorkflowRequest request)
        {
            var response = await PostAsync<WorkflowResultDto>("/api/v1/workflows/execute", request);
            return response.Data;
        }

        /// <summary>
        /// Connects to the agent hub
        /// </summary>
        /// <returns>Whether the connection was successful</returns>
        public async Task<bool> ConnectToAgentHubAsync()
        {
            if (_hubConnection != null)
            {
                return _hubConnection.State == HubConnectionState.Connected;
            }
            
            if (string.IsNullOrEmpty(_token))
            {
                throw new InvalidOperationException("You must authenticate before connecting to the agent hub");
            }
            
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_baseUrl}/hubs/agent", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_token);
                })
                .WithAutomaticReconnect()
                .Build();
            
            try
            {
                await _hubConnection.StartAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Subscribes to agent events
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <param name="onStatusUpdate">Callback for status updates</param>
        /// <param name="onActionEvent">Callback for action events</param>
        /// <returns>Whether the subscription was successful</returns>
        public async Task<bool> SubscribeToAgentEventsAsync(
            string agentId,
            Action<string, string> onStatusUpdate = null,
            Action<string, string, ActionResultDto> onActionEvent = null)
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                var connected = await ConnectToAgentHubAsync();
                if (!connected)
                {
                    return false;
                }
            }
            
            if (onStatusUpdate != null)
            {
                _hubConnection.On<string, string>("AgentStatusUpdate", onStatusUpdate);
            }
            
            if (onActionEvent != null)
            {
                _hubConnection.On<string, string, ActionResultDto>("AgentActionEvent", onActionEvent);
            }
            
            try
            {
                await _hubConnection.InvokeAsync("SubscribeToAgent", agentId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Unsubscribes from agent events
        /// </summary>
        /// <param name="agentId">ID of the agent</param>
        /// <returns>Whether the unsubscription was successful</returns>
        public async Task<bool> UnsubscribeFromAgentEventsAsync(string agentId)
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                return false;
            }
            
            try
            {
                await _hubConnection.InvokeAsync("UnsubscribeFromAgent", agentId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<ApiResponse<T>> GetAsync<T>(string endpoint, Dictionary<string, string> queryParams = null)
        {
            var url = _baseUrl + endpoint;
            
            if (queryParams != null && queryParams.Count > 0)
            {
                var queryString = string.Join("&", queryParams.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
                url += $"?{queryString}";
            }
            
            var response = await _httpClient.GetAsync(url);
            return await DeserializeResponseAsync<T>(response);
        }

        private async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
        {
            var url = _baseUrl + endpoint;
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            return await DeserializeResponseAsync<T>(response);
        }

        private async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint)
        {
            var url = _baseUrl + endpoint;
            var response = await _httpClient.DeleteAsync(url);
            return await DeserializeResponseAsync<T>(response);
        }

        private async Task<ApiResponse<T>> DeserializeResponseAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    return JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                }
                catch (JsonException)
                {
                    return ApiResponse<T>.Error($"Failed to deserialize response: {content}");
                }
            }
            
            return ApiResponse<T>.Error($"HTTP error: {response.StatusCode} - {content}");
        }

        /// <summary>
        /// Disposes the client
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the client
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _hubConnection?.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    _httpClient?.Dispose();
                }
                
                _disposed = true;
            }
        }
    }
}
