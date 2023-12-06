namespace RBTB_ServiceStrategy.Database.Entities
{
	public class Level
	{
		public int Id { get; set; }
		public string Symbol { get; set; }
		public decimal Price { get; set; }
		public decimal Volume { get; set; }
		public DateTime DateCreate { get; set; }
	}
}
