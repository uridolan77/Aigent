using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Aigent.Core
{
    /// <summary>
    /// Generic action implementation
    /// </summary>
    public class GenericAction : IAction
    {
        /// <summary>
        /// Type of the action
        /// </summary>
        public string ActionType { get; }

        /// <summary>
        /// Parameters of the action
        /// </summary>
        public Dictionary<string, object> Parameters { get; }

        /// <summary>
        /// Priority of the action (higher values indicate higher priority)
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Estimated cost of performing the action
        /// </summary>
        public double EstimatedCost { get; }

        /// <summary>
        /// Initializes a new instance of the GenericAction class
        /// </summary>
        /// <param name="actionType">Type of the action</param>
        /// <param name="parameters">Parameters of the action</param>
        /// <param name="priority">Priority of the action</param>
        /// <param name="estimatedCost">Estimated cost of the action</param>
        public GenericAction(string actionType, Dictionary<string, object> parameters, int priority = 0, double estimatedCost = 0.5)
        {
            ActionType = actionType;
            Parameters = parameters ?? new Dictionary<string, object>();
            Priority = priority;
            EstimatedCost = estimatedCost;
        }

        /// <summary>
        /// Executes the action
        /// </summary>
        /// <returns>Result of the action</returns>
        public Task<ActionResult> Execute()
        {
            var stopwatch = Stopwatch.StartNew();

            // Simulate some processing time based on the number of parameters
            if (Parameters.Count > 5)
            {
                Task.Delay(20).Wait(); // More parameters means more complex action
            }
            else if (Parameters.Count > 0)
            {
                Task.Delay(10).Wait(); // Simple action with parameters
            }

            stopwatch.Stop();

            return Task.FromResult(ActionResult.Successful(
                $"Generic action {ActionType} executed",
                Parameters,
                stopwatch.Elapsed));
        }
    }
}
