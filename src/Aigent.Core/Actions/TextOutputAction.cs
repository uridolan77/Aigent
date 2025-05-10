using System.Collections.Generic;
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
        /// Initializes a new instance of the TextOutputAction class
        /// </summary>
        /// <param name="text">Text to output</param>
        public TextOutputAction(string text)
        {
            Parameters = new Dictionary<string, object>
            {
                ["text"] = text
            };
        }
        
        /// <summary>
        /// Executes the action
        /// </summary>
        /// <returns>Result of the action</returns>
        public Task<ActionResult> Execute()
        {
            var text = Parameters["text"]?.ToString() ?? "";
            
            return Task.FromResult(ActionResult.Successful("Text output action executed", new Dictionary<string, object>
            {
                ["text"] = text
            }));
        }
    }
}
