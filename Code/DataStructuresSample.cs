class DataStructureSample
{
	public string Name { get; set; } = "abcd";
	public float Floating { get; set; } = 12.2f;
	public int Integering { get; set; } = 12;
	public bool Booleaning { get; set; } = true;
	// public struct StructSub
	// {
	// 	string Awa = "aaaa";
	// };
	public DataSubStructureSample SubStruct { get; set; }

	public DataStructureSample(string setName = "aaaa")
	{
		Name = setName;
		SubStruct = new DataSubStructureSample( "hello world" );
	}
}
