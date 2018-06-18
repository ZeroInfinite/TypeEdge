﻿
using Microsoft.Azure.IoT.TypeEdge.Modules;
using Microsoft.Azure.IoT.TypeEdge.Modules.Endpoints;
using Microsoft.Azure.IoT.TypeEdge.Modules.Messages;
using System.Collections.Generic;
using ThermostatApplication.Messages;
using ThermostatApplication.Modules;

namespace Modules
{
    public class DataSampling : EdgeModule, IDataSampling
    {
        const int _windowMaxSamples = 1000;
        const int _maxDelayPercentage = 10;

        Queue<Temperature> _sample;

        public Input<Temperature> Temperature { get; set; }
        public Output<Reference<Sample>> Samples { get; set; }

        public DataSampling(IPreprocessor proxy)
        {
            _sample = new Queue<Temperature>();
            Temperature.Subscribe(proxy.Training, async signal =>
            {
                Reference<Sample> message = null;
                lock (_sample)
                {
                    _sample.Enqueue(signal);
                    if (_sample.Count >= _windowMaxSamples)
                    {
                        message = new Reference<Sample>()
                        {
                            Message = new Sample() { Data = _sample.ToArray() }
                        };
                        for (int i = 0; i < _maxDelayPercentage * _windowMaxSamples / 100; i++)
                            _sample.Dequeue();
                    }
                }
                if (message != null)
                    await Samples.PublishAsync(message);

                return MessageResult.Ok;
            });

        }
    }
}
