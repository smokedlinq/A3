﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace A3.Tests.Fixtures
{
    public class Widget : WidgetBase
    {
        private readonly string? name;
        private readonly Widget? parent;

        public Widget()
            : this(null)
        {
        }

        public Widget(string? name)
            => this.name = name ?? nameof(Widget);

        internal Widget(string? name, Widget parent)
            : this(name)
            => this.parent = parent ?? throw new ArgumentNullException(nameof(parent));

        public virtual string? Name
        {
            get
            {
                if (parent is null)
                {
                    return name;
                }

                return $"{parent.Name}.{name}";
            }
        }

        public virtual Task<bool> ExecuteAsync()
            => Task.FromResult(Execute());

        public virtual bool Execute()
            => !(parent is null);

        public async IAsyncEnumerable<Widget> EnumerateAsync()
        {
            await Task.CompletedTask.ConfigureAwait(false);
            var current = this;

            while (!(current is null))
            {
                yield return current;
                current = current.parent;
            }
        }
    }
}
