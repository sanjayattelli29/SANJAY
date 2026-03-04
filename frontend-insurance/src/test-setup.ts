import { vi } from 'vitest';

// Mock IntersectionObserver which is missing in jsdom
class MockIntersectionObserver {
    observe = vi.fn();
    disconnect = vi.fn();
    unobserve = vi.fn();
}

Object.defineProperty(window, 'IntersectionObserver', {
    writable: true,
    configurable: true,
    value: MockIntersectionObserver
});

// Mock SignalR to prevent connection attempts during unit tests
vi.mock('@microsoft/signalr', () => {
    class MockHubConnection {
        start = vi.fn().mockResolvedValue(null);
        stop = vi.fn().mockResolvedValue(null);
        on = vi.fn();
        off = vi.fn();
        invoke = vi.fn().mockResolvedValue(null);
        send = vi.fn().mockResolvedValue(null);
    }

    class MockHubConnectionBuilder {
        withUrl = vi.fn().mockReturnThis();
        withAutomaticReconnect = vi.fn().mockReturnThis();
        build = vi.fn().mockImplementation(() => new MockHubConnection());
    }

    return {
        HubConnectionBuilder: MockHubConnectionBuilder,
        HubConnectionState: {
            Connected: 'Connected',
            Disconnected: 'Disconnected',
            Connecting: 'Connecting',
            Reconnecting: 'Reconnecting',
            DisconnectedWithError: 'DisconnectedWithError'
        },
        HttpTransportType: {
            None: 0,
            WebSockets: 1,
            ServerSentEvents: 2,
            LongPolling: 4
        },
        LogLevel: {
            None: 0,
            Critical: 1,
            Error: 2,
            Warning: 3,
            Information: 4,
            Debug: 5,
            Trace: 6
        }
    };
});
