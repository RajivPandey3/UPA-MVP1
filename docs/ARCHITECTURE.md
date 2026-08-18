# MVP-1 Architecture
UPA.Core -> UPA.ProjectModel -> UPA.Analysis -> UPA.Unity
Core and ProjectModel remain Unity-agnostic. Unity-specific inspection is isolated behind the adapter boundary.
