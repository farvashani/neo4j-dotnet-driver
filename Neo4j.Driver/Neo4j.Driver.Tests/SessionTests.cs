﻿//  Copyright (c) 2002-2016 "Neo Technology,"
//  Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
//  This file is part of Neo4j.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
using System;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Exceptions;
using Neo4j.Driver.Internal.result;
using Xunit;

namespace Neo4j.Driver.Tests
{
    public class SessionTests
    {
        public class RunMethod
        {
            [Fact]
            public void ShouldSyncOnRun()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = new Session(null, null, mockConn.Object);
                session.Run("lalalal");

                mockConn.Verify(x => x.Run(It.IsAny<ResultBuilder>(), "lalalal", null), Times.Once);
                mockConn.Verify(x => x.PullAll(It.IsAny<ResultBuilder>()), Times.Once);
                mockConn.Verify(x=>x.SyncAsync());
            }

        }

        public class BeginTransactionMethod
        {
            [Fact]
            public void ShouldNotAllowNewTxWhileOneIsRunning()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = new Session(null, null, mockConn.Object);
                session.BeginTransaction();
                var error = Xunit.Record.Exception(() => session.BeginTransaction());
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public void ShouldBeAbleToOpenTxAfterPreviousIsClosed()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = new Session(null, null, mockConn.Object);
                var tx = session.BeginTransaction();
                tx.Dispose();
                tx = session.BeginTransaction();
            }

            [Fact]
            public void ShouldNotBeAbleToUseSessionWhileOngoingTransaction()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = new Session(null, null, mockConn.Object);
                var tx = session.BeginTransaction();

                var error = Xunit.Record.Exception(() => session.Run("lalal"));
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public void ShouldBeAbleToUseSessionAgainWhenTransactionIsClosed()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = new Session(null, null, mockConn.Object);
                var tx = session.BeginTransaction();
                tx.Dispose();

                session.Run("lalal");
            }

            [Fact]
            public void ShouldNotAllowMoreStatementsInSessionWhileConnectionClosed()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(false);
                var session = new Session(null, null, mockConn.Object);

                var error = Xunit.Record.Exception(() => session.Run("lalal"));
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public void ShouldNotAllowMoreTransactionsInSessionWhileConnectionClosed()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(false);
                var session = new Session(null, null, mockConn.Object);

                var error = Xunit.Record.Exception(() => session.BeginTransaction());
                error.Should().BeOfType<ClientException>();
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void ShouldDisposeConnOnDispose()
            {
                var mockConn = new Mock<IConnection>();
                var session = new Session(null, null, mockConn.Object);
                session.Dispose();

                mockConn.Verify(x => x.Dispose(), Times.Once);
            }

            [Fact]
            public void ShouldDisposeTxAndConnOnDispose()
            {
                var mockConn = new Mock<IConnection>();
                mockConn.Setup(x => x.IsOpen).Returns(true);
                var session = new Session(null, null, mockConn.Object);
                var tx = session.BeginTransaction();
                session.Dispose();

                mockConn.Verify(x => x.Dispose(), Times.Once);
                mockConn.Verify(x=>x.Run(null, "ROLLBACK", null), Times.Once);

            }
        }
    }
}
