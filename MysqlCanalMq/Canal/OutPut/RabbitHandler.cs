﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MysqlCanalMq.Common.Produce.RabbitMq;
using MysqlCanalMq.Models;
using Polly;

namespace MysqlCanalMq.Canal.OutPut
{
    public class RabbitHandler : INotificationHandler<CanalBody>
    {
        private readonly ILogger _logger;
        private readonly RabitMqOption _rabitMqOption;
        public RabbitHandler(ILogger<RabbitHandler> logger, IOptions<RabitMqOption> rabbitMqOption)
        {
            _logger = logger;
            _rabitMqOption = rabbitMqOption.Value;
            if (rabbitMqOption == null)
            {
                throw new ArgumentNullException("Rabbit in appsettings.json is empty!");
            }

            if (string.IsNullOrEmpty(_rabitMqOption.Host) || string.IsNullOrEmpty(_rabitMqOption.UserName) ||
                string.IsNullOrEmpty(_rabitMqOption.Password) || _rabitMqOption.Port < 1)
            {
                throw new ArgumentNullException("Rabbit param in appsettings.json is not correct!");
            }
        }

        public Task Handle(CanalBody notification, CancellationToken cancellationToken)
        {
            var ploicy = Policy.Handle<Exception>()
                .WaitAndRetry(new[]
                {
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(4),
                    TimeSpan.FromSeconds(6),
                    TimeSpan.FromSeconds(8),
                    TimeSpan.FromSeconds(15),
                    TimeSpan.FromSeconds(30)
                });
            try
            {
                var _produceRabbitMq = OutPutFactory.CreateRabitMqProduce(_rabitMqOption);
                ploicy.Execute(() => _produceRabbitMq.Produce(notification.Message));
            }
            catch (Exception)
            {
                _logger.LogError("rabbit mq send fail");
                throw;
            }
            return Task.CompletedTask;
        }

    }



}
