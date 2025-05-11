namespace Aigent.Core.Interfaces
{
    /// <summary>
    /// Interface for machine learning models used by agents
    /// </summary>
    public interface IMLModel
    {
        /// <summary>
        /// Gets the name of the model
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the version of the model
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// Predicts an output based on the input
        /// </summary>
        /// <typeparam name="TInput">Type of the input</typeparam>
        /// <typeparam name="TOutput">Type of the output</typeparam>
        /// <param name="input">Input data</param>
        /// <returns>Predicted output</returns>
        TOutput Predict<TInput, TOutput>(TInput input);
    }
}
