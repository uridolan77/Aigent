// Real-World Agent Examples
using Microsoft.ML;
using Microsoft.Cognitive Services.Speech;
using Slack.Webhooks;
using SendGrid;
using Twilio;
using Azure.AI.TextAnalytics;

namespace AgentSystem.Examples.RealWorld
{
    // Customer Support Agent with NLP
    public class CustomerSupportAgent : BaseAgent
    {
        public override AgentType Type => AgentType.Hybrid;
        private readonly TextAnalyticsClient _textAnalytics;
        private readonly IMLModel _intentClassifier;
        private readonly ISendGridClient _emailClient;
        private readonly ITwilioClient _smsClient;

        public CustomerSupportAgent(
            string name,
            TextAnalyticsClient textAnalytics,
            IMLModel intentClassifier,
            ISendGridClient emailClient,
            ITwilioClient smsClient,
            IMemoryService memory,
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus)
            : base(memory, safetyValidator, logger, messageBus)
        {
            Name = name;
            _textAnalytics = textAnalytics;
            _intentClassifier = intentClassifier;
            _emailClient = emailClient;
            _smsClient = smsClient;
        }

        public override async Task<IAction> DecideAction(EnvironmentState state)
        {
            if (!state.Properties.TryGetValue("customer_query", out var query))
                return new NoOpAction();

            var queryText = query.ToString();
            
            // Analyze sentiment
            var sentimentResult = await _textAnalytics.AnalyzeSentimentAsync(queryText);
            var sentiment = sentimentResult.Value.DocumentSentiment;
            
            // Extract key phrases
            var keyPhrasesResult = await _textAnalytics.ExtractKeyPhrasesAsync(queryText);
            var keyPhrases = keyPhrasesResult.Value.ToList();
            
            // Classify intent
            var intent = await ClassifyIntent(queryText);
            
            // Store context
            await _memory.StoreContext($"customer_{state.Properties["customer_id"]}_sentiment", sentiment);
            await _memory.StoreContext($"customer_{state.Properties["customer_id"]}_intent", intent);
            
            // Route based on intent and sentiment
            return await RouteToAppropriateAction(intent, sentiment, state);
        }

        private async Task<string> ClassifyIntent(string text)
        {
            var prediction = await _intentClassifier.Predict(new { Text = text });
            if (prediction is Dictionary<string, object> result)
            {
                return result["intent"]?.ToString() ?? "unknown";
            }
            return "unknown";
        }

        private async Task<IAction> RouteToAppropriateAction(
            string intent, 
            DocumentSentiment sentiment,
            EnvironmentState state)
        {
            // Handle urgent/negative cases
            if (sentiment.Sentiment == TextSentiment.Negative && sentiment.ConfidenceScores.Negative > 0.8)
            {
                _logger.LogWarning($"High priority case detected: negative sentiment {sentiment.ConfidenceScores.Negative}");
                return new EscalateToHumanAction
                {
                    Priority = Priority.High,
                    Reason = "Strong negative sentiment detected",
                    Context = state.Properties
                };
            }

            // Route based on intent
            switch (intent.ToLower())
            {
                case "order_status":
                    return await HandleOrderStatusQuery(state);
                    
                case "technical_support":
                    return await HandleTechnicalSupport(state);
                    
                case "billing_inquiry":
                    return await HandleBillingInquiry(state);
                    
                case "product_recommendation":
                    return await HandleProductRecommendation(state);
                    
                default:
                    return new TextOutputAction(
                        "I'm here to help! Could you provide more details about your inquiry?");
            }
        }

        private async Task<IAction> HandleOrderStatusQuery(EnvironmentState state)
        {
            var customerId = state.Properties["customer_id"].ToString();
            var orderHistory = await _memory.RetrieveContext<List<Order>>($"orders_{customerId}");
            
            if (orderHistory == null || !orderHistory.Any())
            {
                return new TextOutputAction("I couldn't find any recent orders. Please provide your order number.");
            }

            var recentOrder = orderHistory.OrderByDescending(o => o.Date).First();
            return new MultiChannelResponseAction
            {
                Channels = new List<ResponseChannel>
                {
                    new ResponseChannel
                    {
                        Type = ChannelType.Chat,
                        Message = $"Your order #{recentOrder.Id} is {recentOrder.Status}. Expected delivery: {recentOrder.ExpectedDelivery:MMM dd}"
                    },
                    new ResponseChannel
                    {
                        Type = ChannelType.Email,
                        Message = GenerateOrderStatusEmail(recentOrder)
                    }
                }
            };
        }

        private string GenerateOrderStatusEmail(Order order)
        {
            return $@"
Dear Customer,

Your order #{order.Id} is currently {order.Status}.

Order Details:
- Order Date: {order.Date:yyyy-MM-dd}
- Expected Delivery: {order.ExpectedDelivery:yyyy-MM-dd}
- Tracking Number: {order.TrackingNumber}

You can track your order at: https://example.com/track/{order.TrackingNumber}

Thank you for your business!

Best regards,
Customer Support Team
";
        }
    }

    // Financial Trading Agent
    public class TradingAgent : BaseAgent
    {
        public override AgentType Type => AgentType.Deliberative;
        private readonly IMarketDataService _marketData;
        private readonly ITradingService _tradingService;
        private readonly IRiskManager _riskManager;
        private readonly IMLModel _pricePredictor;

        public TradingAgent(
            string name,
            IMarketDataService marketData,
            ITradingService tradingService,
            IRiskManager riskManager,
            IMLModel pricePredictor,
            IMemoryService memory,
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus)
            : base(memory, safetyValidator, logger, messageBus)
        {
            Name = name;
            _marketData = marketData;
            _tradingService = tradingService;
            _riskManager = riskManager;
            _pricePredictor = pricePredictor;
            
            Capabilities = new AgentCapabilities
            {
                SupportedActionTypes = new List<string> 
                { 
                    "BuyOrder", "SellOrder", "MarketAnalysis", "RiskAssessment" 
                },
                SkillLevels = new Dictionary<string, double>
                {
                    ["market_analysis"] = 0.9,
                    ["risk_management"] = 0.95,
                    ["execution_speed"] = 0.85
                }
            };
        }

        public override async Task<IAction> DecideAction(EnvironmentState state)
        {
            if (!state.Properties.TryGetValue("symbol", out var symbol))
                return new NoOpAction();

            var tradingSymbol = symbol.ToString();
            
            // Get market data
            var marketData = await _marketData.GetQuote(tradingSymbol);
            var historicalData = await _marketData.GetHistoricalData(tradingSymbol, TimeSpan.FromDays(30));
            
            // Perform technical analysis
            var technicalIndicators = CalculateTechnicalIndicators(historicalData);
            
            // Get ML predictions
            var prediction = await PredictPriceMovement(marketData, historicalData, technicalIndicators);
            
            // Check risk constraints
            var currentPosition = await _tradingService.GetPosition(tradingSymbol);
            var riskAssessment = await _riskManager.AssessRisk(currentPosition, prediction);
            
            // Store analysis for audit
            await _memory.StoreContext($"analysis_{tradingSymbol}_{DateTime.UtcNow:yyyyMMddHHmmss}", 
                new MarketAnalysis
                {
                    Symbol = tradingSymbol,
                    Timestamp = DateTime.UtcNow,
                    CurrentPrice = marketData.LastPrice,
                    Prediction = prediction,
                    TechnicalIndicators = technicalIndicators,
                    RiskAssessment = riskAssessment
                });
            
            // Make trading decision
            return await MakeTradingDecision(tradingSymbol, prediction, riskAssessment, currentPosition);
        }

        private TechnicalIndicators CalculateTechnicalIndicators(List<HistoricalQuote> data)
        {
            var indicators = new TechnicalIndicators();
            
            // Moving averages
            indicators.SMA20 = CalculateSMA(data, 20);
            indicators.SMA50 = CalculateSMA(data, 50);
            indicators.EMA12 = CalculateEMA(data, 12);
            indicators.EMA26 = CalculateEMA(data, 26);
            
            // MACD
            indicators.MACD = indicators.EMA12 - indicators.EMA26;
            indicators.Signal = CalculateEMA(GetMACDHistory(data), 9);
            
            // RSI
            indicators.RSI = CalculateRSI(data, 14);
            
            // Bollinger Bands
            var (upper, middle, lower) = CalculateBollingerBands(data, 20, 2);
            indicators.BollingerUpper = upper;
            indicators.BollingerMiddle = middle;
            indicators.BollingerLower = lower;
            
            return indicators;
        }

        private async Task<PricePrediction> PredictPriceMovement(
            MarketQuote currentData,
            List<HistoricalQuote> historicalData,
            TechnicalIndicators indicators)
        {
            var features = new
            {
                CurrentPrice = currentData.LastPrice,
                Volume = currentData.Volume,
                Volatility = CalculateVolatility(historicalData),
                RSI = indicators.RSI,
                MACD = indicators.MACD,
                BollingerPosition = (currentData.LastPrice - indicators.BollingerLower) / 
                                    (indicators.BollingerUpper - indicators.BollingerLower)
            };
            
            var prediction = await _pricePredictor.Predict(features);
            
            if (prediction is Dictionary<string, object> result)
            {
                return new PricePrediction
                {
                    Direction = Enum.Parse<PriceDirection>(result["direction"].ToString()),
                    Magnitude = Convert.ToDouble(result["magnitude"]),
                    Confidence = Convert.ToDouble(result["confidence"]),
                    TimeHorizon = TimeSpan.FromMinutes(Convert.ToInt32(result["horizon_minutes"]))
                };
            }
            
            return new PricePrediction { Direction = PriceDirection.Neutral };
        }

        private async Task<IAction> MakeTradingDecision(
            string symbol,
            PricePrediction prediction,
            RiskAssessment risk,
            Position currentPosition)
        {
            // Don't trade if risk is too high
            if (risk.RiskScore > 0.8)
            {
                _logger.LogWarning($"Risk too high for {symbol}: {risk.RiskScore}");
                return new NoOpAction();
            }
            
            // Don't trade on low confidence predictions
            if (prediction.Confidence < 0.7)
            {
                return new NoOpAction();
            }
            
            // Calculate position size based on risk
            var positionSize = _riskManager.CalculatePositionSize(risk, prediction);
            
            // Execute trade based on prediction
            if (prediction.Direction == PriceDirection.Up && prediction.Magnitude > 0.5)
            {
                if (currentPosition.Quantity <= 0)
                {
                    return new BuyOrderAction
                    {
                        Symbol = symbol,
                        Quantity = positionSize,
                        OrderType = OrderType.Limit,
                        LimitPrice = currentPosition.LastPrice * 1.001, // 0.1% above market
                        TimeInForce = TimeInForce.IOC
                    };
                }
            }
            else if (prediction.Direction == PriceDirection.Down && prediction.Magnitude > 0.5)
            {
                if (currentPosition.Quantity > 0)
                {
                    return new SellOrderAction
                    {
                        Symbol = symbol,
                        Quantity = Math.Min(positionSize, currentPosition.Quantity),
                        OrderType = OrderType.Limit,
                        LimitPrice = currentPosition.LastPrice * 0.999, // 0.1% below market
                        TimeInForce = TimeInForce.IOC
                    };
                }
            }
            
            return new NoOpAction();
        }

        private double CalculateSMA(List<HistoricalQuote> data, int period)
        {
            return data.TakeLast(period).Average(d => d.Close);
        }

        private double CalculateEMA(List<HistoricalQuote> data, int period)
        {
            var multiplier = 2.0 / (period + 1);
            var ema = data.First().Close;
            
            foreach (var quote in data.Skip(1))
            {
                ema = (quote.Close - ema) * multiplier + ema;
            }
            
            return ema;
        }

        private double CalculateRSI(List<HistoricalQuote> data, int period)
        {
            var gains = new List<double>();
            var losses = new List<double>();
            
            for (int i = 1; i < data.Count; i++)
            {
                var change = data[i].Close - data[i - 1].Close;
                if (change > 0)
                {
                    gains.Add(change);
                    losses.Add(0);
                }
                else
                {
                    gains.Add(0);
                    losses.Add(Math.Abs(change));
                }
            }
            
            var avgGain = gains.TakeLast(period).Average();
            var avgLoss = losses.TakeLast(period).Average();
            
            if (avgLoss == 0)
                return 100;
                
            var rs = avgGain / avgLoss;
            return 100 - (100 / (1 + rs));
        }

        private (double upper, double middle, double lower) CalculateBollingerBands(
            List<HistoricalQuote> data, int period, double stdDev)
        {
            var sma = CalculateSMA(data, period);
            var variance = data.TakeLast(period).Select(d => Math.Pow(d.Close - sma, 2)).Average();
            var standardDeviation = Math.Sqrt(variance);
            
            return (
                sma + stdDev * standardDeviation,
                sma,
                sma - stdDev * standardDeviation
            );
        }

        private double CalculateVolatility(List<HistoricalQuote> data)
        {
            var returns = new List<double>();
            for (int i = 1; i < data.Count; i++)
            {
                returns.Add((data[i].Close - data[i - 1].Close) / data[i - 1].Close);
            }
            
            return Math.Sqrt(returns.Select(r => Math.Pow(r, 2)).Average()) * Math.Sqrt(252);
        }

        private List<HistoricalQuote> GetMACDHistory(List<HistoricalQuote> data)
        {
            // Simplified - would need to calculate MACD for each historical point
            return data;
        }
    }

    // IoT Device Management Agent
    public class IoTManagementAgent : BaseAgent
    {
        public override AgentType Type => AgentType.Reactive;
        private readonly IIoTHubService _iotHub;
        private readonly IDeviceRegistry _deviceRegistry;
        private readonly ITelemetryProcessor _telemetryProcessor;
        private readonly IAlertService _alertService;

        public IoTManagementAgent(
            string name,
            IIoTHubService iotHub,
            IDeviceRegistry deviceRegistry,
            ITelemetryProcessor telemetryProcessor,
            IAlertService alertService,
            IMemoryService memory,
            ISafetyValidator safetyValidator,
            ILogger logger,
            IMessageBus messageBus)
            : base(memory, safetyValidator, logger, messageBus)
        {
            Name = name;
            _iotHub = iotHub;
            _deviceRegistry = deviceRegistry;
            _telemetryProcessor = telemetryProcessor;
            _alertService = alertService;
            
            // Subscribe to device events
            _messageBus.Subscribe("iot.device.telemetry", HandleDeviceTelemetry);
            _messageBus.Subscribe("iot.device.alert", HandleDeviceAlert);
        }

        public override async Task<IAction> DecideAction(EnvironmentState state)
        {
            if (state.Properties.TryGetValue("event_type", out var eventType))
            {
                switch (eventType.ToString())
                {
                    case "device_registration":
                        return await HandleDeviceRegistration(state);
                        
                    case "telemetry_anomaly":
                        return await HandleTelemetryAnomaly(state);
                        
                    case "maintenance_required":
                        return await HandleMaintenanceRequest(state);
                        
                    case "firmware_update":
                        return await HandleFirmwareUpdate(state);
                        
                    default:
                        return new NoOpAction();
                }
            }
            
            return new NoOpAction();
        }

        private async Task<IAction> HandleDeviceRegistration(EnvironmentState state)
        {
            var deviceId = state.Properties["device_id"].ToString();
            var deviceType = state.Properties["device_type"].ToString();
            
            // Validate device
            var isValid = await ValidateDevice(deviceId, deviceType);
            if (!isValid)
            {
                return new DeviceRegistrationAction
                {
                    DeviceId = deviceId,
                    Action = RegistrationAction.Reject,
                    Reason = "Device validation failed"
                };
            }
            
            // Register device
            var device = await _deviceRegistry.RegisterDevice(new Device
            {
                Id = deviceId,
                Type = deviceType,
                RegisteredAt = DateTime.UtcNow,
                Status = DeviceStatus.Active
            });
            
            // Configure device
            var config = GenerateDeviceConfiguration(device);
            await _iotHub.SendConfiguration(deviceId, config);
            
            return new DeviceRegistrationAction
            {
                DeviceId = deviceId,
                Action = RegistrationAction.Accept,
                Configuration = config
            };
        }

        private async Task<IAction> HandleTelemetryAnomaly(EnvironmentState state)
        {
            var deviceId = state.Properties["device_id"].ToString();
            var telemetry = JsonSerializer.Deserialize<DeviceTelemetry>(
                state.Properties["telemetry"].ToString());
            
            // Analyze anomaly
            var analysis = await _telemetryProcessor.AnalyzeAnomaly(telemetry);
            
            // Store anomaly for historical analysis
            await _memory.StoreContext($"anomaly_{deviceId}_{DateTime.UtcNow:yyyyMMddHHmmss}", analysis);
            
            // Determine severity and action
            if (analysis.Severity == AnomalySeverity.Critical)
            {
                // Create high-priority alert
                await _alertService.CreateAlert(new Alert
                {
                    DeviceId = deviceId,
                    Type = AlertType.CriticalAnomaly,
                    Message = $"Critical anomaly detected: {analysis.Description}",
                    Severity = AlertSeverity.Critical,
                    Timestamp = DateTime.UtcNow
                });
                
                // Take immediate action
                return new DeviceCommandAction
                {
                    DeviceId = deviceId,
                    Command = "emergency_shutdown",
                    Parameters = new Dictionary<string, object>
                    {
                        ["reason"] = analysis.Description,
                        ["anomaly_id"] = analysis.Id
                    }
                };
            }
            else if (analysis.Severity == AnomalySeverity.Warning)
            {
                // Schedule maintenance
                return new ScheduleMaintenanceAction
                {
                    DeviceId = deviceId,
                    Reason = analysis.Description,
                    ScheduledFor = DateTime.UtcNow.AddDays(7),
                    Priority = MaintenancePriority.Normal
                };
            }
            
            // Monitor the situation
            return new MonitorDeviceAction
            {
                DeviceId = deviceId,
                Duration = TimeSpan.FromHours(24),
                Metrics = new List<string> { "temperature", "pressure", "vibration" }
            };
        }

        private async Task<IAction> HandleMaintenanceRequest(EnvironmentState state)
        {
            var deviceId = state.Properties["device_id"].ToString();
            var maintenanceType = state.Properties["maintenance_type"].ToString();
            
            // Check device history
            var maintenanceHistory = await _memory.RetrieveContext<List<MaintenanceRecord>>(
                $"maintenance_history_{deviceId}");
            
            // Determine if maintenance is due
            var lastMaintenance = maintenanceHistory?.LastOrDefault(m => m.Type == maintenanceType);
            var isDue = lastMaintenance == null || 
                       DateTime.UtcNow - lastMaintenance.CompletedAt > GetMaintenanceInterval(maintenanceType);
            
            if (isDue)
            {
                return new ScheduleMaintenanceAction
                {
                    DeviceId = deviceId,
                    MaintenanceType = maintenanceType,
                    ScheduledFor = DateTime.UtcNow.AddDays(3),
                    EstimatedDuration = GetMaintenanceDuration(maintenanceType),
                    RequiredParts = GetRequiredParts(maintenanceType)
                };
            }
            
            return new NoOpAction();
        }

        private async Task<IAction> HandleFirmwareUpdate(EnvironmentState state)
        {
            var deviceId = state.Properties["device_id"].ToString();
            var targetVersion = state.Properties["target_version"].ToString();
            
            // Get device info
            var device = await _deviceRegistry.GetDevice(deviceId);
            
            // Check compatibility
            var isCompatible = await CheckFirmwareCompatibility(device, targetVersion);
            
            if (!isCompatible)
            {
                _logger.LogWarning($"Firmware {targetVersion} incompatible with device {deviceId}");
                return new NoOpAction();
            }
            
            // Schedule update during maintenance window
            var maintenanceWindow = await GetNextMaintenanceWindow(deviceId);
            
            return new FirmwareUpdateAction
            {
                DeviceId = deviceId,
                TargetVersion = targetVersion,
                ScheduledFor = maintenanceWindow,
                BackupCurrentVersion = true,
                RollbackOnFailure = true
            };
        }

        private void HandleDeviceTelemetry(object message)
        {
            if (message is DeviceTelemetryMessage telemetry)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        // Process telemetry
                        var processed = await _telemetryProcessor.ProcessTelemetry(telemetry);
                        
                        // Store for analysis
                        await _memory.StoreContext(
                            $"telemetry_{telemetry.DeviceId}_{DateTime.UtcNow:yyyyMMddHHmmss}", 
                            processed);
                        
                        // Check for anomalies
                        if (processed.HasAnomaly)
                        {
                            var state = new EnvironmentState
                            {
                                Properties = new Dictionary<string, object>
                                {
                                    ["event_type"] = "telemetry_anomaly",
                                    ["device_id"] = telemetry.DeviceId,
                                    ["telemetry"] = processed
                                }
                            };
                            
                            await DecideAction(state);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing telemetry for device {telemetry.DeviceId}");
                    }
                });
            }
        }

        private void HandleDeviceAlert(object message)
        {
            if (message is DeviceAlert alert)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogWarning($"Device alert: {alert.DeviceId} - {alert.Message}");
                        
                        // Create action based on alert type
                        var state = new EnvironmentState
                        {
                            Properties = new Dictionary<string, object>
                            {
                                ["event_type"] = MapAlertToEventType(alert.Type),
                                ["device_id"] = alert.DeviceId,
                                ["alert"] = alert
                            }
                        };
                        
                        await DecideAction(state);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error handling alert for device {alert.DeviceId}");
                    }
                });
            }
        }

        private async Task<bool> ValidateDevice(string deviceId, string deviceType)
        {
            // Implement device validation logic
            return true;
        }

        private DeviceConfiguration GenerateDeviceConfiguration(Device device)
        {
            return new DeviceConfiguration
            {
                DeviceId = device.Id,
                TelemetryInterval = TimeSpan.FromMinutes(5),
                AlertThresholds = GetDefaultThresholds(device.Type),
                EnabledFeatures = GetDefaultFeatures(device.Type)
            };
        }

        private TimeSpan GetMaintenanceInterval(string maintenanceType)
        {
            return maintenanceType switch
            {
                "routine" => TimeSpan.FromDays(90),
                "calibration" => TimeSpan.FromDays(180),
                "deep_clean" => TimeSpan.FromDays(365),
                _ => TimeSpan.FromDays(30)
            };
        }

        private TimeSpan GetMaintenanceDuration(string maintenanceType)
        {
            return maintenanceType switch
            {
                "routine" => TimeSpan.FromHours(2),
                "calibration" => TimeSpan.FromHours(4),
                "deep_clean" => TimeSpan.FromHours(8),
                _ => TimeSpan.FromHours(1)
            };
        }

        private List<string> GetRequiredParts(string maintenanceType)
        {
            // Return list of required parts based on maintenance type
            return new List<string>();
        }

        private async Task<bool> CheckFirmwareCompatibility(Device device, string targetVersion)
        {
            // Implement compatibility check
            return true;
        }

        private async Task<DateTime> GetNextMaintenanceWindow(string deviceId)
        {
            // Find next available maintenance window
            return DateTime.UtcNow.AddDays(7).Date.AddHours(2); // 2 AM in 7 days
        }

        private Dictionary<string, double> GetDefaultThresholds(string deviceType)
        {
            return new Dictionary<string, double>
            {
                ["temperature_max"] = 85.0,
                ["temperature_min"] = -10.0,
                ["pressure_max"] = 150.0,
                ["vibration_max"] = 5.0
            };
        }

        private List<string> GetDefaultFeatures(string deviceType)
        {
            return new List<string> { "telemetry", "remote_control", "auto_update" };
        }

        private string MapAlertToEventType(AlertType alertType)
        {
            return alertType switch
            {
                AlertType.Maintenance => "maintenance_required",
                AlertType.Anomaly => "telemetry_anomaly",
                AlertType.Offline => "device_offline",
                _ => "unknown"
            };
        }
    }

    // Supporting classes and interfaces
    public interface IMarketDataService
    {
        Task<MarketQuote> GetQuote(string symbol);
        Task<List<HistoricalQuote>> GetHistoricalData(string symbol, TimeSpan period);
    }

    public interface ITradingService
    {
        Task<Position> GetPosition(string symbol);
        Task<OrderResult> PlaceOrder(OrderRequest order);
    }

    public interface IRiskManager
    {
        Task<RiskAssessment> AssessRisk(Position position, PricePrediction prediction);
        int CalculatePositionSize(RiskAssessment risk, PricePrediction prediction);
    }

    public interface IIoTHubService
    {
        Task SendConfiguration(string deviceId, DeviceConfiguration config);
        Task SendCommand(string deviceId, DeviceCommand command);
    }

    public interface IDeviceRegistry
    {
        Task<Device> RegisterDevice(Device device);
        Task<Device> GetDevice(string deviceId);
        Task UpdateDevice(Device device);
    }

    public interface ITelemetryProcessor
    {
        Task<ProcessedTelemetry> ProcessTelemetry(DeviceTelemetryMessage telemetry);
        Task<AnomalyAnalysis> AnalyzeAnomaly(DeviceTelemetry telemetry);
    }

    public interface IAlertService
    {
        Task CreateAlert(Alert alert);
        Task<List<Alert>> GetAlerts(string deviceId, TimeSpan period);
    }

    // Action classes
    public class EscalateToHumanAction : IAction
    {
        public string ActionType => "EscalateToHuman";
        public Priority Priority { get; set; }
        public string Reason { get; set; }
        public Dictionary<string, object> Context { get; set; }
        public Dictionary<string, object> Parameters => new()
        {
            ["priority"] = Priority,
            ["reason"] = Reason,
            ["context"] = Context
        };
    }

    public class MultiChannelResponseAction : IAction
    {
        public string ActionType => "MultiChannelResponse";
        public List<ResponseChannel> Channels { get; set; }
        public Dictionary<string, object> Parameters => new()
        {
            ["channels"] = Channels
        };
    }

    public class BuyOrderAction : IAction
    {
        public string ActionType => "BuyOrder";
        public string Symbol { get; set; }
        public int Quantity { get; set; }
        public OrderType OrderType { get; set; }
        public decimal? LimitPrice { get; set; }
        public TimeInForce TimeInForce { get; set; }
        public Dictionary<string, object> Parameters => new()
        {
            ["symbol"] = Symbol,
            ["quantity"] = Quantity,
            ["order_type"] = OrderType,
            ["limit_price"] = LimitPrice,
            ["time_in_force"] = TimeInForce
        };
    }

    public class SellOrderAction : IAction
    {
        public string ActionType => "SellOrder";
        public string Symbol { get; set; }
        public int Quantity { get; set; }
        public OrderType OrderType { get; set; }
        public decimal? LimitPrice { get; set; }
        public TimeInForce TimeInForce { get; set; }
        public Dictionary<string, object> Parameters => new()
        {
            ["symbol"] = Symbol,
            ["quantity"] = Quantity,
            ["order_type"] = OrderType,
            ["limit_price"] = LimitPrice,
            ["time_in_force"] = TimeInForce
        };
    }

    public class DeviceCommandAction : IAction
    {
        public string ActionType => "DeviceCommand";
        public string DeviceId { get; set; }
        public string Command { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }

    public class ScheduleMaintenanceAction : IAction
    {
        public string ActionType => "ScheduleMaintenance";
        public string DeviceId { get; set; }
        public string MaintenanceType { get; set; }
        public string Reason { get; set; }
        public DateTime ScheduledFor { get; set; }
        public TimeSpan EstimatedDuration { get; set; }
        public List<string> RequiredParts { get; set; }
        public MaintenancePriority Priority { get; set; }
        public Dictionary<string, object> Parameters => new()
        {
            ["device_id"] = DeviceId,
            ["maintenance_type"] = MaintenanceType,
            ["reason"] = Reason,
            ["scheduled_for"] = ScheduledFor,
            ["estimated_duration"] = EstimatedDuration,
            ["required_parts"] = RequiredParts,
            ["priority"] = Priority
        };
    }

    // Enums and supporting types
    public enum Priority { Low, Normal, High, Critical }
    public enum ChannelType { Chat, Email, SMS, Push }
    public enum OrderType { Market, Limit, Stop, StopLimit }
    public enum TimeInForce { Day, GTC, IOC, FOK }
    public enum PriceDirection { Up, Down, Neutral }
    public enum AnomalySeverity { Low, Medium, Warning, Critical }
    public enum AlertType { Info, Warning, Error, Critical, Maintenance, Anomaly, Offline }
    public enum AlertSeverity { Low, Medium, High, Critical }
    public enum MaintenancePriority { Low, Normal, High, Urgent }
    public enum DeviceStatus { Active, Inactive, Maintenance, Error }
    public enum RegistrationAction { Accept, Reject, Pending }

    // Data structures
    public class Order
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public DateTime ExpectedDelivery { get; set; }
        public string TrackingNumber { get; set; }
    }

    public class ResponseChannel
    {
        public ChannelType Type { get; set; }
        public string Message { get; set; }
    }

    public class MarketQuote
    {
        public string Symbol { get; set; }
        public decimal LastPrice { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public long Volume { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class HistoricalQuote
    {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
    }

    public class TechnicalIndicators
    {
        public double SMA20 { get; set; }
        public double SMA50 { get; set; }
        public double EMA12 { get; set; }
        public double EMA26 { get; set; }
        public double MACD { get; set; }
        public double Signal { get; set; }
        public double RSI { get; set; }
        public double BollingerUpper { get; set; }
        public double BollingerMiddle { get; set; }
        public double BollingerLower { get; set; }
    }

    public class PricePrediction
    {
        public PriceDirection Direction { get; set; }
        public double Magnitude { get; set; }
        public double Confidence { get; set; }
        public TimeSpan TimeHorizon { get; set; }
    }

    public class RiskAssessment
    {
        public double RiskScore { get; set; }
        public string Description { get; set; }
        public Dictionary<string, double> RiskFactors { get; set; }
    }

    public class Position
    {
        public string Symbol { get; set; }
        public int Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal LastPrice { get; set; }
        public decimal UnrealizedPnL { get; set; }
    }

    public class MarketAnalysis
    {
        public string Symbol { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal CurrentPrice { get; set; }
        public PricePrediction Prediction { get; set; }
        public TechnicalIndicators TechnicalIndicators { get; set; }
        public RiskAssessment RiskAssessment { get; set; }
    }

    public class Device
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DeviceStatus Status { get; set; }
        public string FirmwareVersion { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }

    public class DeviceConfiguration
    {
        public string DeviceId { get; set; }
        public TimeSpan TelemetryInterval { get; set; }
        public Dictionary<string, double> AlertThresholds { get; set; }
        public List<string> EnabledFeatures { get; set; }
    }

    public class DeviceTelemetry
    {
        public string DeviceId { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, double> Metrics { get; set; }
    }

    public class DeviceTelemetryMessage
    {
        public string DeviceId { get; set; }
        public DateTime Timestamp { get; set; }
        public DeviceTelemetry Telemetry { get; set; }
    }

    public class ProcessedTelemetry
    {
        public DeviceTelemetry Original { get; set; }
        public Dictionary<string, double> NormalizedMetrics { get; set; }
        public bool HasAnomaly { get; set; }
        public List<string> AnomalyTypes { get; set; }
    }

    public class AnomalyAnalysis
    {
        public string Id { get; set; }
        public string DeviceId { get; set; }
        public DateTime Timestamp { get; set; }
        public AnomalySeverity Severity { get; set; }
        public string Description { get; set; }
        public Dictionary<string, double> AnomalousMetrics { get; set; }
    }

    public class Alert
    {
        public string Id { get; set; }
        public string DeviceId { get; set; }
        public AlertType Type { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DeviceAlert
    {
        public string DeviceId { get; set; }
        public AlertType Type { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class MaintenanceRecord
    {
        public string Id { get; set; }
        public string DeviceId { get; set; }
        public string Type { get; set; }
        public DateTime ScheduledAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public string TechnicianId { get; set; }
        public List<string> PartsUsed { get; set; }
        public string Notes { get; set; }
    }

    public class FirmwareUpdateAction : IAction
    {
        public string ActionType => "FirmwareUpdate";
        public string DeviceId { get; set; }
        public string TargetVersion { get; set; }
        public DateTime ScheduledFor { get; set; }
        public bool BackupCurrentVersion { get; set; }
        public bool RollbackOnFailure { get; set; }
        public Dictionary<string, object> Parameters => new()
        {
            ["device_id"] = DeviceId,
            ["target_version"] = TargetVersion,
            ["scheduled_for"] = ScheduledFor,
            ["backup_current"] = BackupCurrentVersion,
            ["rollback_on_failure"] = RollbackOnFailure
        };
    }

    public class MonitorDeviceAction : IAction
    {
        public string ActionType => "MonitorDevice";
        public string DeviceId { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> Metrics { get; set; }
        public Dictionary<string, object> Parameters => new()
        {
            ["device_id"] = DeviceId,
            ["duration"] = Duration,
            ["metrics"] = Metrics
        };
    }

    public class DeviceRegistrationAction : IAction
    {
        public string ActionType => "DeviceRegistration";
        public string DeviceId { get; set; }
        public RegistrationAction Action { get; set; }
        public string Reason { get; set; }
        public DeviceConfiguration Configuration { get; set; }
        public Dictionary<string, object> Parameters => new()
        {
            ["device_id"] = DeviceId,
            ["action"] = Action,
            ["reason"] = Reason,
            ["configuration"] = Configuration
        };
    }
}
