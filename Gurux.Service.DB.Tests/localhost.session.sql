DECLARE @agentId UNIQUEIDENTIFIER = 'c39592dd-6bfa-477d-97ac-6c108f3f8a75';
DECLARE @deviceId UNIQUEIDENTIFIER = NULL;
IF COL_LENGTH('GXTask', 'Batck') IS NOT NULL BEGIN WITH [CandidateTasks] AS (
    SELECT [GXTask].[Id] AS [TaskId],
        [GXTask].[CreationTime] AS [CreationTime],
        [GXTask].[Batck] AS [TaskBatch]
    FROM [GXTask] [GXTask]
        INNER JOIN [GXDevice] [GXDevice] ON [GXTask].[Device] = [GXDevice].[Id]
    WHERE [GXTask].[Start] IS NULL
        AND NOT EXISTS (
            SELECT 1
            FROM [GXTask] [RunningTask]
            WHERE [RunningTask].[Start] IS NOT NULL
                AND [RunningTask].[Device] = [GXDevice].[Id]
        )
        AND (
            (
                @deviceId IS NOT NULL
                AND [GXTask].[Device] = @deviceId
            )
            OR (
                @deviceId IS NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM [GXAgent] [GXAgent]
                        INNER JOIN [GXAgentGroupAgent] [GXAgentGroupAgent] ON [GXAgentGroupAgent].[GXAgentID] = [GXAgent].[Id]
                        INNER JOIN [GXAgentGroup] [GXAgentGroup] ON [GXAgentGroup].[Id] = [GXAgentGroupAgent].[AgentGroupID]
                        INNER JOIN [GXAgentGroupDeviceGroup] [GXAgentGroupDeviceGroup] ON [GXAgentGroupDeviceGroup].[AgentGroupId] = [GXAgentGroup].[Id]
                        INNER JOIN [GXDeviceGroup] [GXDeviceGroup] ON [GXDeviceGroup].[Id] = [GXAgentGroupDeviceGroup].[DeviceGroupId]
                        INNER JOIN [GXDeviceGroupDevice] [GXDeviceGroupDevice] ON [GXDeviceGroupDevice].[DeviceGroupID] = [GXDeviceGroup].[Id]
                        INNER JOIN [GXDevice] [MappedDevice] ON [MappedDevice].[Id] = [GXDeviceGroupDevice].[DeviceID]
                    WHERE [GXAgent].[Id] = @agentId
                        AND [MappedDevice].[Id] = [GXTask].[Device]
                )
            )
        )
        AND (
            [GXTask].[OperatingAgent] = @agentId
            OR (
                [GXTask].[OperatingAgent] IS NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM [GXTask] [AgentTask]
                    WHERE [AgentTask].[Start] IS NULL
                        AND [AgentTask].[Device] = [GXTask].[Device]
                        AND [AgentTask].[OperatingAgent] = @agentId
                )
            )
        )
),
[OldestTask] AS (
    SELECT TOP (1) [TaskId],
        [TaskBatch]
    FROM [CandidateTasks]
    ORDER BY [CreationTime]
)
SELECT [c].[TaskId] AS [GXTask.Id]
FROM [CandidateTasks] [c]
    CROSS JOIN [OldestTask] [o]
WHERE [c].[TaskId] = [o].[TaskId]
    OR (
        [o].[TaskBatch] IS NOT NULL
        AND [c].[TaskBatch] = [o].[TaskBatch]
    )
ORDER BY [c].[CreationTime];
END
ELSE IF COL_LENGTH('GXTask', 'Batch') IS NOT NULL BEGIN WITH [CandidateTasks] AS (
    SELECT [GXTask].[Id] AS [TaskId],
        [GXTask].[CreationTime] AS [CreationTime],
        [GXTask].[Batch] AS [TaskBatch]
    FROM [GXTask] [GXTask]
        INNER JOIN [GXDevice] [GXDevice] ON [GXTask].[Device] = [GXDevice].[Id]
    WHERE [GXTask].[Start] IS NULL
        AND NOT EXISTS (
            SELECT 1
            FROM [GXTask] [RunningTask]
            WHERE [RunningTask].[Start] IS NOT NULL
                AND [RunningTask].[Device] = [GXDevice].[Id]
        )
        AND (
            (
                @deviceId IS NOT NULL
                AND [GXTask].[Device] = @deviceId
            )
            OR (
                @deviceId IS NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM [GXAgent] [GXAgent]
                        INNER JOIN [GXAgentGroupAgent] [GXAgentGroupAgent] ON [GXAgentGroupAgent].[GXAgentID] = [GXAgent].[Id]
                        INNER JOIN [GXAgentGroup] [GXAgentGroup] ON [GXAgentGroup].[Id] = [GXAgentGroupAgent].[AgentGroupID]
                        INNER JOIN [GXAgentGroupDeviceGroup] [GXAgentGroupDeviceGroup] ON [GXAgentGroupDeviceGroup].[AgentGroupId] = [GXAgentGroup].[Id]
                        INNER JOIN [GXDeviceGroup] [GXDeviceGroup] ON [GXDeviceGroup].[Id] = [GXAgentGroupDeviceGroup].[DeviceGroupId]
                        INNER JOIN [GXDeviceGroupDevice] [GXDeviceGroupDevice] ON [GXDeviceGroupDevice].[DeviceGroupID] = [GXDeviceGroup].[Id]
                        INNER JOIN [GXDevice] [MappedDevice] ON [MappedDevice].[Id] = [GXDeviceGroupDevice].[DeviceID]
                    WHERE [GXAgent].[Id] = @agentId
                        AND [MappedDevice].[Id] = [GXTask].[Device]
                )
            )
        )
        AND (
            [GXTask].[OperatingAgent] = @agentId
            OR (
                [GXTask].[OperatingAgent] IS NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM [GXTask] [AgentTask]
                    WHERE [AgentTask].[Start] IS NULL
                        AND [AgentTask].[Device] = [GXTask].[Device]
                        AND [AgentTask].[OperatingAgent] = @agentId
                )
            )
        )
),
[OldestTask] AS (
    SELECT TOP (1) [TaskId],
        [TaskBatch]
    FROM [CandidateTasks]
    ORDER BY [CreationTime]
)
SELECT [c].[TaskId] AS [GXTask.Id]
FROM [CandidateTasks] [c]
    CROSS JOIN [OldestTask] [o]
WHERE [c].[TaskId] = [o].[TaskId]
    OR (
        [o].[TaskBatch] IS NOT NULL
        AND [c].[TaskBatch] = [o].[TaskBatch]
    )
ORDER BY [c].[CreationTime];
END
ELSE BEGIN WITH [CandidateTasks] AS (
    SELECT [GXTask].[Id] AS [TaskId],
        [GXTask].[CreationTime] AS [CreationTime]
    FROM [GXTask] [GXTask]
        INNER JOIN [GXDevice] [GXDevice] ON [GXTask].[Device] = [GXDevice].[Id]
    WHERE [GXTask].[Start] IS NULL
        AND NOT EXISTS (
            SELECT 1
            FROM [GXTask] [RunningTask]
            WHERE [RunningTask].[Start] IS NOT NULL
                AND [RunningTask].[Device] = [GXDevice].[Id]
        )
        AND (
            (
                @deviceId IS NOT NULL
                AND [GXTask].[Device] = @deviceId
            )
            OR (
                @deviceId IS NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM [GXAgent] [GXAgent]
                        INNER JOIN [GXAgentGroupAgent] [GXAgentGroupAgent] ON [GXAgentGroupAgent].[GXAgentID] = [GXAgent].[Id]
                        INNER JOIN [GXAgentGroup] [GXAgentGroup] ON [GXAgentGroup].[Id] = [GXAgentGroupAgent].[AgentGroupID]
                        INNER JOIN [GXAgentGroupDeviceGroup] [GXAgentGroupDeviceGroup] ON [GXAgentGroupDeviceGroup].[AgentGroupId] = [GXAgentGroup].[Id]
                        INNER JOIN [GXDeviceGroup] [GXDeviceGroup] ON [GXDeviceGroup].[Id] = [GXAgentGroupDeviceGroup].[DeviceGroupId]
                        INNER JOIN [GXDeviceGroupDevice] [GXDeviceGroupDevice] ON [GXDeviceGroupDevice].[DeviceGroupID] = [GXDeviceGroup].[Id]
                        INNER JOIN [GXDevice] [MappedDevice] ON [MappedDevice].[Id] = [GXDeviceGroupDevice].[DeviceID]
                    WHERE [GXAgent].[Id] = @agentId
                        AND [MappedDevice].[Id] = [GXTask].[Device]
                )
            )
        )
        AND (
            [GXTask].[OperatingAgent] = @agentId
            OR (
                [GXTask].[OperatingAgent] IS NULL
                AND NOT EXISTS (
                    SELECT 1
                    FROM [GXTask] [AgentTask]
                    WHERE [AgentTask].[Start] IS NULL
                        AND [AgentTask].[Device] = [GXTask].[Device]
                        AND [AgentTask].[OperatingAgent] = @agentId
                )
            )
        )
)
SELECT TOP (1) [TaskId] AS [GXTask.Id]
FROM [CandidateTasks]
ORDER BY [CreationTime];
END