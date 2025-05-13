using Confluent.Kafka;
using System.Transactions;

namespace Acm.Api.Helpers
{
    public static class TransactionUtils
    {
        public static void Produce(IProducer<Null, string> producer, Transaction transaction)
        {
            //producer.Produce(ConstConfiguration.TopicAapEvent,
            //    new Message<Null, string>
            //    {
            //        Value = transaction.ToSerializeObjectIgnoreLoop()
            //    });
        }
    }
}
