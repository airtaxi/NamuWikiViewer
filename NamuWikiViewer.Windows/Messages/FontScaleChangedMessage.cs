using CommunityToolkit.Mvvm.Messaging.Messages;
using NamuWikiViewer.Commons.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NamuWikiViewer.Windows.Messages;

public class FontScaleChangedMessage(double fontScale) : ValueChangedMessage<double>(fontScale);
