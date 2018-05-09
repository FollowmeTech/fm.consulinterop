using Grpc.Core;
using System;
using System.Collections.Generic;

namespace FM.ConsulInterop
{
    internal sealed class ClientCallInvoker : CallInvoker
    {
        Channel grpcChannel;
        ClientCallActionCollection _callActionCollection;

        public ClientCallInvoker(Channel channel)
        {
            this.grpcChannel = channel;
        }

        public ClientCallInvoker(Channel channel, ClientCallActionCollection clientCallActionCollection) : this(channel)
        {
            this._callActionCollection = clientCallActionCollection;
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method,
            string host, CallOptions options, TRequest request)
        {
            ServerCallInvoker callInvoker = new ServerCallInvoker(grpcChannel);
            TResponse response = default(TResponse);

            try
            {
                _callActionCollection?.ForEach(callAction =>
                {
                    InnerLogger.Log(LoggerLevel.Debug, "pre:" + callAction.GetType().Name);
                    options = (CallOptions)callAction?.PreAction(method, host, options, request);
                    InnerLogger.Log(LoggerLevel.Debug, "end :" + callAction.GetType().Name);
                });

                response = callInvoker.BlockingUnaryCall(method, host, options, request);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _callActionCollection?.ForEach(callAction =>
                {
                    InnerLogger.Log(LoggerLevel.Debug, "post:" + callAction.GetType().Name);
                    callAction?.PostAction(response);
                    InnerLogger.Log(LoggerLevel.Debug, "end:" + callAction.GetType().Name);
                });
            }

            return response;
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            ServerCallInvoker callInvoker = new ServerCallInvoker(grpcChannel);
            var response = default(AsyncUnaryCall<TResponse>);

            try
            {
                _callActionCollection?.ForEach(callAction =>
                {
                    InnerLogger.Log(LoggerLevel.Debug, "pre:" + callAction.GetType().Name);
                    options = (CallOptions)callAction?.PreAction(method, host, options, request);
                    InnerLogger.Log(LoggerLevel.Debug, "end :" + callAction.GetType().Name);
                });

                response = callInvoker.AsyncUnaryCall(method, host, (CallOptions)options, request);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {                                                    
                _callActionCollection?.ForEach(callAction =>
                {
                    InnerLogger.Log(LoggerLevel.Debug, "post:" + callAction.GetType().Name);
                    callAction?.PostAction(response);
                    InnerLogger.Log(LoggerLevel.Debug, "end:" + callAction.GetType().Name);
                });
            }

            return response;
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options,
            TRequest request)
        {
            throw new NotImplementedException("FM.ConsulInterop 未实现");
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            throw new NotImplementedException("FM.ConsulInterop 未实现");
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            throw new NotImplementedException("FM.ConsulInterop 未实现");
        }
    }
}
