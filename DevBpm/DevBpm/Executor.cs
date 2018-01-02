using System;
using System.Web;
using Terrasoft.Core;
using Terrasoft.Core.Entities;

namespace DevBpm
{
	internal class Executor : IExecutor
	{
		public void Execute(UserConnection userConnection) {
			HttpContext.Current.Response.Write(Environment.NewLine);
			var entitySchema = userConnection.EntitySchemaManager.GetInstanceByName("Contact");
			var esq = new EntitySchemaQuery(entitySchema);
			esq.AddAllSchemaColumns();
			var collection = esq.GetEntityCollection(userConnection);
			foreach (var entity in collection) {
				HttpContext.Current.Response.Write(entity.GetTypedColumnValue<string>("Name"));
				HttpContext.Current.Response.Write(Environment.NewLine);
			}
		}
	}

}