﻿using A3.Act;
using A3.Fixtures;
using AutoFixture;
using AutoFixture.Kernel;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace A3.Arrange
{
    public sealed class ArrangeBuilder<T>
        where T : class
    {
        private readonly List<Func<ArrangeContext, T>> factory = new List<Func<ArrangeContext, T>>();
        private readonly List<Mock> mocks = new List<Mock>();
        private readonly List<SutParameter> parameter = new List<SutParameter>();

        internal ArrangeBuilder(ConstructorInfo? constructor)
            => factory.Add(context => CreateConstructorSutFactory(context, constructor));

        public IFixture Fixture { get; } = new Fixture().Customize(new AutoFixtureCustomization());

        public ArrangeBuilder<T> Mock<TMock>(Action<Mock<TMock>> setup)
            where TMock : class
        {
            var mock = GetOrAddMock<TMock>();
            setup(mock);
            return this;
        }

        public ArrangeBuilder<T> Mock<TMock>()
            where TMock : class
        {
            _ = GetOrAddMock<TMock>();
            return this;
        }

        private Mock<TMock> GetOrAddMock<TMock>()
            where TMock : class
        {
            var mock = (Mock<TMock>)mocks.FirstOrDefault(x => x is Mock<TMock>);

            if (mock is null)
            {
                mock = Fixture.Create<Mock<TMock>>();
                mocks.Add(mock);
            }

            return mock;
        }

        public ArrangeBuilder<T> Sut(Func<Type, ConstructorInfo?> selector)
        {
            _ = selector ?? throw new ArgumentNullException(nameof(selector));
            factory.Add(context => CreateConstructorSutFactory(context, selector(typeof(T))));
            return this;
        }

        public ArrangeBuilder<T> Sut(Func<ArrangeContext, T> factory)
        {
            _ = factory ?? throw new ArgumentNullException(nameof(factory));
            this.factory.Add(factory);
            return this;
        }

        public ArrangeBuilder<T> Parameter<TParameter>(TParameter parameter)
        {
            this.parameter.Add(new SutParameter(typeof(TParameter), parameter));
            return this;
        }

        internal ActStep<T> Build()
            => new ActStep<T>(() => factory.Last().Invoke(new ArrangeContext(Fixture, mocks)), parameter.LastOrDefault(), mocks);

        private static T CreateConstructorSutFactory(ArrangeContext context, ConstructorInfo? constructor)
        {
            _ = constructor ?? throw new ArrangeException($"Could not instantiate {typeof(T)}; verify it is not an interface or abstract, or that the Sut is explicitly configured during the arrange step.");

            var parameters = constructor.GetParameters();
            var args = new object[parameters.Length];
            var fixture = new SpecimenContext(context.Fixture);

            for (var i = 0; i < parameters.Length; i++)
            {
                var ctorParameter = parameters[i];

                if (context.TryGetMock(ctorParameter.ParameterType, out var mock))
                {
                    args[i] = mock.Object;
                }
                else
                {
                    try
                    {
                        args[i] = fixture.Resolve(ctorParameter.ParameterType);
                    }
                    catch (ObjectCreationException)
                    {
                        args[i] = ((Mock)fixture.Resolve(typeof(Mock<>).MakeGenericType(ctorParameter.ParameterType))).Object;
                    }
                }
            }

            return (T)constructor.Invoke(args);
        }
    }
}