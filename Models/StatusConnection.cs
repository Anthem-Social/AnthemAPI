using Amazon.DynamoDBv2.DataModel;

[DynamoDBTable("StatusConnections")]
public class StatusConnection
{
    [DynamoDBHashKey]
    public required string UserId { get; set; }
    public required HashSet<string> ConnectionIds { get; set; }
}