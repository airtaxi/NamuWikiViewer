using CommunityToolkit.Mvvm.Messaging.Messages;
using NamuWikiViewer.Commons.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NamuWikiViewer.Windows.Messages;

public class PendingPageAddedMessage(PendingPage pendingPage) : ValueChangedMessage<PendingPage>(pendingPage);
