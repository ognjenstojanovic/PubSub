using System;
using FluentAssertions;
using PubSub.Solution;
using Xunit;

namespace PubSub.Tests
{
    public class PubSubTests
    {
        private readonly IPubSub<int> _pubSub;

        public PubSubTests()
        {
            _pubSub = new SimplePubSub<int>();
        }

        /// <summary>
        /// Subscribing to a topic should receive message from that topic.
        /// </summary>
        [Fact]
        public void PublishSimpleTest()
        {
            var recorder = new Recorder<int>();

            _pubSub.Subscribe("/temperature", recorder.Handler);
            _pubSub.Publish("/temperature", 25);

            var expected = Tuple.Create("/temperature", 25);

            recorder.Messages.Should()
                .ContainSingle(tuple => Equals(tuple, expected));
        }

        /// <summary>
        /// When message is published before any subscribers are attached, no messages should be received.
        /// </summary>
        [Fact]
        public void PublishBeforeSubscribe_ReceivesNoMessages()
        {
            var recorder = new Recorder<int>();

            _pubSub.Publish("/temperature", 25);
            _pubSub.Subscribe("/temperature", recorder.Handler);

            recorder.Messages.Should().BeEmpty();
        }

        /// <summary>
        /// Publishing to different topic receives no messages.
        /// </summary>
        [Fact]
        public void PublishingToDifferentTopic_ReceivesNoMessages()
        {
            var recorder = new Recorder<int>();

            _pubSub.Subscribe("/other", recorder.Handler);
            _pubSub.Publish("/temperature", 25);

            recorder.Messages.Should().BeEmpty();
        }

        /// <summary>
        /// Two subscribers for a different topic should receive intended messages.
        /// </summary>
        [Fact]
        public void PublishSimple_TwoSubscribers_Test()
        {
            var ones = new Recorder<int>();
            var twos = new Recorder<int>();

            _pubSub.Subscribe("/temperature", ones.Handler);
            _pubSub.Subscribe("/humidity", twos.Handler);

            _pubSub.Publish("/temperature", 25);
            _pubSub.Publish("/humidity", 125);

            ones.Messages.Should()
                .ContainSingle(tuple => Equals(tuple, Tuple.Create("/temperature", 25)));

            twos.Messages.Should()
                .ContainSingle(tuple => Equals(tuple, Tuple.Create("/humidity", 125)));
        }

        /// <summary>
        /// Subscribers to wildcard topics should receive all matching messages
        /// </summary>
        [Fact]
        public void SubscribedToWildcard_ReceivesMessage()
        {
            var recorder = new Recorder<int>();

            _pubSub.Subscribe("/home/bedroom/+", recorder.Handler);

            _pubSub.Publish("/home/bedroom/temperature", 30);
            _pubSub.Publish("/home/bedroom/humidity", 40);

            // should receive all home/bedroom messages
            var tempMessage = Tuple.Create("/home/bedroom/temperature", 30);
            var humMessage = Tuple.Create("/home/bedroom/humidity", 40);

            recorder.Messages.Should()
                .HaveCount(2).And
                .Contain(tuple => Equals(tuple, tempMessage)).And
                .Contain(tuple => Equals(tuple, humMessage));
        }

        /// <summary>
        /// Subscribers with wildcard in the middle of the topic should receive matching messages.
        /// </summary>
        [Fact]
        public void SubscribedToWildcard_InMiddle_ReceivesMessage()
        {
            var recorder = new Recorder<int>();

            _pubSub.Subscribe("/home/+/temperature", recorder.Handler);

            _pubSub.Publish("/home/bedroom/temperature", 30);
            _pubSub.Publish("/home/garage/temperature", 40);

            _pubSub.Publish("/home/bedroom/humidity", 100); // should fail, humidity sent

            // should receive all temperature messages, regardless of location
            var tempMessage = Tuple.Create("/home/bedroom/temperature", 30);
            var humMessage = Tuple.Create("/home/garage/temperature", 40);

            recorder.Messages.Should()
                .HaveCount(2).And
                .Contain(tuple => Equals(tuple, tempMessage)).And
                .Contain(tuple => Equals(tuple, humMessage));
        }

        [Fact]
        public void SubscribedWithMultipleWildcards_ReceivesMessage()
        {
            var recorder = new Recorder<int>();

            _pubSub.Subscribe("/home/+/temperature/+", recorder.Handler);

            _pubSub.Publish("/home/bedroom/temperature/celsius", 30);
            _pubSub.Publish("/home/garage/temperature/fahrenheit", 40);

            _pubSub.Publish("/home/garage/temperature", -1);            // should fail, incomplete topic
            _pubSub.Publish("/home/garage/humidity/rh", -1);            // should fail, 'humidity' sent
            _pubSub.Publish("/office/garage/temperature/celsius", -1);  // should fail, 'office' sent
            
            // should receive all 'home' messages with all 'temperature' units
            var tempMessage = Tuple.Create("/home/bedroom/temperature/celsius", 30);
            var humMessage = Tuple.Create("/home/garage/temperature/fahrenheit", 40);

            recorder.Messages.Should()
                .HaveCount(2).And
                .Contain(tuple => Equals(tuple, tempMessage)).And
                .Contain(tuple => Equals(tuple, humMessage));
        }

        /// <summary>
        /// Subscribing to wildcard # receives all messages
        /// </summary>
        [Fact]
        public void SubscribedToWildcard_WithPound_ReceivesMessage()
        {
            var recorder = new Recorder<int>();

            _pubSub.Subscribe("/home/#", recorder.Handler);

            _pubSub.Publish("/home/bedroom/temperature", 30);
            _pubSub.Publish("/home/bedroom/humidity", 40);

            _pubSub.Publish("/office/table/humidity", 50);  // should fail, message sent to 'office'

            var tempMessage = Tuple.Create("/home/bedroom/temperature", 30);
            var humMessage = Tuple.Create("/home/bedroom/humidity", 40);

            recorder.Messages.Should()
                .HaveCount(2).And
                .Contain(tuple => Equals(tuple, tempMessage)).And
                .Contain(tuple => Equals(tuple, humMessage));
        }

        /// <summary>
        /// Subscribing to wild combination of wildcards.
        /// </summary>
        [Fact]
        public void SubscribedToWildcard_WithPound_AndPlus_ReceivesMessage()
        {
            var recorder = new Recorder<int>();

            _pubSub.Subscribe("/group/+/home/+/#", recorder.Handler);

            _pubSub.Publish("/group/1/home/bedroom/humidity", 80);
            _pubSub.Publish("/group/1/home/bedroom/temperature", 30);
            _pubSub.Publish("/group/1/home/bedroom/temperature/celsius", 30);
            _pubSub.Publish("/group/1/home/bedroom/temperature/fahrenheit", 30);

            _pubSub.Publish("/home/bedroom/humidity", 40);              // should fail, 'home' sent
            _pubSub.Publish("/office/table/humidity", 50);              // should fail, 'office' sent
            _pubSub.Publish("/group/1/office/bedroom/humidity", -1);    // should fail, 'office' sent where 'home' is expected

            var tempMessage = Tuple.Create("/group/1/home/bedroom/temperature", 30);
            var tempCelsiusMessage = Tuple.Create("/group/1/home/bedroom/temperature/celsius", 30);
            var tempFahrenheitMessage = Tuple.Create("/group/1/home/bedroom/temperature/fahrenheit", 30);
            var humMessage = Tuple.Create("/group/1/home/bedroom/humidity", 80);

            recorder.Messages.Should()
                .HaveCount(4).And
                .Contain(tuple => Equals(tuple, humMessage)).And
                .Contain(tuple => Equals(tuple, tempMessage)).And
                .Contain(tuple => Equals(tuple, tempCelsiusMessage)).And
                .Contain(tuple => Equals(tuple, tempFahrenheitMessage));
        }

        [Theory]
        [InlineData("/hello")]
        [InlineData("/hello/world")]
        [InlineData("/hello/+")]
        [InlineData("/hello/#")]
        [InlineData("/+/#")]
        public void SubscribeToValidTopic_Success(string topic)
        {
            var recorder = new Recorder<int>();
            _pubSub.Subscribe(topic, recorder.Handler);
        }

        [Theory]
        [InlineData("/hello")]
        [InlineData("/hello/world")]
        public void PublishToValidTopic_Success(string topic)
        {
            _pubSub.Publish(topic, 0);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("invalid")]
        [InlineData("invalid/")]
        [InlineData("/home/")]
        [InlineData("//home")]
        [InlineData("/home/#")]
        public void PublishToInvalidTopic_Fails(string topic)
        {
            var ex = Assert.Throws<InvalidTopicException>(() => _pubSub.Publish(topic, 0));
            Assert.NotNull(ex);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("invalid")]
        [InlineData("invalid/")]
        [InlineData("/home/")]
        [InlineData("//home")]
        [InlineData("/home/##")]
        [InlineData("/home/temp+")]
        [InlineData("/home/temp#")]
        [InlineData("/+/##")]
        [InlineData("/home/#/#")]
        [InlineData("/home/#/+")]
        [InlineData("/#/+")]
        public void SubscribeToInvalidTopic_Fails(string topic)
        {
            var ex = Assert.Throws<InvalidTopicException>(() => _pubSub.Subscribe(topic, (t, m) => { }));
            Assert.NotNull(ex);
        }
    }
}