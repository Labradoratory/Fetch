﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Labradoratory.DataAccess.Processors.DataPackages;

namespace Labradoratory.DataAccess.Processors
{
    /// <summary>
    /// The <see cref="ProcessorPipeline"/> is used to manage processing of <see cref="DataPackage"/> objects.
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public class ProcessorPipeline
    {
        private AsyncLocal<DataPackage> Current { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessorPipeline"/> class.
        /// </summary>
        /// <param name="processorProvider">The processor provider which is used to discover processors.</param>
        public ProcessorPipeline(IProcessorProvider processorProvider)
        {
            ProcessorProvider = processorProvider;
        }

        /// <summary>
        /// Gets the provider of <see cref="DataPackage"/> processors.
        /// </summary>
        protected IProcessorProvider ProcessorProvider { get; }

        /// <summary>
        /// Processes the provided <see cref="DataPackage"/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="DataPackage"/> to process.</typeparam>
        /// <param name="package">The package to process.</param>
        /// <returns>The task.</returns>
        public async Task ProcessAsync<T>(T package) where T : DataPackage
        {
            // Keep track of the processing chain on a per thread branch basis.
            // On the first process call, the Current value will be NULL.
            package.Previous = Current.Value;
            // Set the current package being processed.
            // By doing this, we create a branch of a tree that can be walked by a processor
            // to determine what initiated the processing.
            Current.Value = package;
            
            foreach (var processor in ProcessorProvider.GetProcessors<T>().OrderBy(p => p.Priority))
            {
                await processor.ProcessAsync(package);
            }
        }
    }        
}
