using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Aigent.Core
{
    /// <summary>
    /// Action that outputs text
    /// </summary>
    public class TextOutputAction : IAction
    {
        /// <summary>
        /// Type of the action
        /// </summary>
        public string ActionType => "TextOutput";

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
        /// Initializes a new instance of the TextOutputAction class
        /// </summary>
        /// <param name="text">Text to output</param>
        /// <param name="priority">Priority of the action</param>
        /// <param name="estimatedCost">Estimated cost of the action</param>
        public TextOutputAction(string text, int priority = 0, double estimatedCost = 0.1)
        {
            Parameters = new Dictionary<string, object>
            {
                ["text"] = text
            };
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
            var text = Parameters["text"]?.ToString() ?? "";

            // Simulate some processing time
            if (text.Length > 100)
            {
                Task.Delay(10).Wait(); // Longer text takes more time to process
            }

            stopwatch.Stop();

            return Task.FromResult(ActionResult.Successful(
                "Text output action executed",
                new Dictionary<string, object> { ["text"] = text },
                stopwatch.Elapsed));
        }
    }
}
