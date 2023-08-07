namespace RabbitMQ.Reception;

public class Reception
{
	public Guid Id { get; set; }
	public Guid PhysicalCustomerId { get; set; }
	public Guid AreaId { get; set; }
	public DateTime CreatedAt { get; set; } // required for a job to be able to check and cancel Receptions that were not fully paid
	public DateTime From { get; set; }
	public DateTime To { get; set; }
	public ReceptionStatus Status { get; set; }
	public ReceptionPaymentStatus PaymentStatus { get; set; }
}

public class ReceptionArea
{
	public Guid Id { get; set; }
	public Guid AreaId { get; set; }
	public ICollection<ReceptionAreaActivity> Activities { get; set; }
	public byte[] Version { get; set; } // concurrencyToken || isRowVersion
}

public class ReceptionAreaActivity
{
	public Guid Id { get; set; }
	// required to bind this activity to some other resource (contract, reception, anything)
	// so that when event is sent to cancel this activity, we would know which one to complete exactly
	// like if there were 3 acitivities created for a single area, but for different time frames
	// event should inherit from CorrelateByGuid interface or smth
	public Guid CorrelationId { get; set; }
	public Guid ReceptionAreaId { get; set; }
	public DateTime From { get; set; }
	public DateTime To { get; set; }
	public bool IsCompleted { get; set; }
	public ReceptionAreaStatus Status { get; set; }
}

public enum ReceptionStatus
{
	Created, // initial creation
	Active, // when the time comes
	Cancelled, // when it's cancelled manually or by a job when reservation wasnt neither confirmed or fully paid
	Completed // succesully completed
}

public enum ReceptionPaymentStatus
{
	Reserved, // Not fully paid
	Paid // Fully paid
}

public enum ReceptionAreaStatus
{
	Reserved, // Not fully paid
	Occupied, // Fully paid
}
