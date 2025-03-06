using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace LLama.Rag
{
    interface IEmbeddingHandler
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns a DataTable of all embeddings</returns>
        Task<DataTable> getAllEmbeddings();

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns a DataTable of all embedding sources</returns>
        Task<DataTable> getAllEmbeddingSources();

        //Need to implement
        Task<DataTable> getEmbeddingsBySourceID(int Id);

        Task setEmbeddings(EmbeddingSource Source, bool Overwrite);

    }
}
