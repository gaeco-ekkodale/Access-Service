// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { toast } from 'sonner'
import {
	AccessRightDTO,
	AccessRightsService,
	GuidelineDTO,
	PropertyRight,
	UserGroupDTO,
} from '../../../api/AccessService'
import { UseCaseDB } from '../../../api/UseCaseService'
import {
	areAccessRightsEqual,
	countAccessRightChanges,
	sortAccessRights,
	upsertAccessRightsDraft,
} from '../../../utils/accessRightsDraft'
import { useAccessRights } from '../../ClassificationPanel/hooks/useAccessRights'
import RightFilter from '../../Filter/components/RightFilter'
import Tour from '../../tour/Tour'
import { TOUR_KEY, TOUR_MODULE_NAME, TOUR_PANELS } from '../../tour/tourContent'
import SelectBar from '../../SelectBar/components/SelectBar'
import {
	useClassifications,
	useClassificationsByGuideline,
	useGuidelines,
	useUseCases,
	useUserGroups,
} from '../hooks'
import {
	DataCheck,
	ErrorPage,
	LoadingPage,
	MainPage,
	NoDataPage,
	NoUserGroupOrUseCasePage,
} from '../ProjectModuleStates/components'
import { ProjectModuleState } from '../ProjectModuleStates/enums/ProjectModuleState'

type PendingSelection =
	| { type: 'useCase'; value: UseCaseDB }
	| { type: 'userGroup'; value: UserGroupDTO }

function ProjectModule() {
	const [selectedUseCase, setSelectedUseCase] = useState<UseCaseDB>()
	const [selectedUserGroup, setSelectedUser] = useState<UserGroupDTO>()
	const [selectedGuideline, setSelectedGuideline] = useState<GuidelineDTO>()
	const [selectedRights, setSelectedRights] = useState<PropertyRight[]>([])
	const [originalAccessRights, setOriginalAccessRights] = useState<AccessRightDTO[]>([])
	const [draftAccessRights, setDraftAccessRights] = useState<AccessRightDTO[]>([])
	const [pendingSelection, setPendingSelection] = useState<PendingSelection | null>(null)
	const [isUnsavedDialogOpen, setIsUnsavedDialogOpen] = useState(false)

	const [uiState, setUiState] = useState<ProjectModuleState>(ProjectModuleState.LOADING)
	const [useCaseLoading, setUseCaseLoading] = useState<boolean>(true)
	const [useCaseError, setUseCaseError] = useState<boolean>(false)
	const [useCaseData, setUseCaseData] = useState<UseCaseDB[] | undefined>(undefined)

	const { isLoading: isAllLoading, isError: isAllError, data: allData } = useClassifications()
	const {
		isLoading: isGuidelineLoading,
		isError: isGuidelineError,
		data: guidelineData,
	} = useClassificationsByGuideline(selectedGuideline?.id ?? undefined)
	const isLoading = selectedGuideline?.id ? isGuidelineLoading : isAllLoading
	const isError = selectedGuideline?.id ? isGuidelineError : isAllError
	const data = selectedGuideline?.id ? guidelineData : allData
	const queryClient = useQueryClient()
	const currentUseCaseId = selectedUseCase?.id ?? ''
	const currentUserGroupId = selectedUserGroup?.id ?? ''
	const currentContextKey =
		currentUseCaseId && currentUserGroupId ? `${currentUseCaseId}::${currentUserGroupId}` : ''
	const { data: accessRightsData } = useAccessRights(currentUseCaseId, currentUserGroupId)

	const hasData = useCallback(
		<T,>(array: T[] | undefined): boolean => Array.isArray(array) && array.length !== 0,
		[],
	)
	const hasUncommittedChanges = useMemo(
		() => !areAccessRightsEqual(originalAccessRights, draftAccessRights),
		[draftAccessRights, originalAccessRights],
	)
	const changedAccessRightsCount = useMemo(
		() => countAccessRightChanges(originalAccessRights, draftAccessRights),
		[draftAccessRights, originalAccessRights],
	)

	const { mutateAsync: commitAccessRightsMutation, isPending: isCommittingAccessRights } =
		useMutation({
			mutationFn: async (accessRights: AccessRightDTO[]) => {
				if (!currentUseCaseId || !currentUserGroupId) {
					return []
				}

				return AccessRightsService.commitAccessRightsAsync(currentUseCaseId, currentUserGroupId, {
					accessRights,
				})
			},
		})

	useEffect(() => {
		if (!currentContextKey) {
			setOriginalAccessRights([])
			setDraftAccessRights([])
			return
		}

		if (hasUncommittedChanges || !accessRightsData) {
			return
		}

		const normalizedAccessRights = sortAccessRights(accessRightsData)
		setOriginalAccessRights(normalizedAccessRights)
		setDraftAccessRights(normalizedAccessRights)
	}, [accessRightsData, currentContextKey, hasUncommittedChanges])

	const clearDraftState = useCallback(() => {
		setOriginalAccessRights([])
		setDraftAccessRights([])
	}, [])

	const applyPendingSelection = useCallback(
		(selection: PendingSelection) => {
			clearDraftState()

			if (selection.type === 'useCase') {
				setSelectedUseCase(selection.value)
				return
			}

			setSelectedUser(selection.value)
		},
		[clearDraftState],
	)

	const requestContextChange = useCallback(
		(selection: PendingSelection) => {
			const isSameSelection =
				selection.type === 'useCase'
					? selectedUseCase?.id === selection.value.id
					: selectedUserGroup?.id === selection.value.id

			if (isSameSelection) {
				return
			}

			if (hasUncommittedChanges) {
				setPendingSelection(selection)
				setIsUnsavedDialogOpen(true)
				return
			}

			applyPendingSelection(selection)
		},
		[applyPendingSelection, hasUncommittedChanges, selectedUseCase?.id, selectedUserGroup?.id],
	)

	const handleUseCaseChange = (useCase: UseCaseDB) => {
		requestContextChange({ type: 'useCase', value: useCase })
	}

	const handleUserGroupChange = (user: UserGroupDTO) => {
		requestContextChange({ type: 'userGroup', value: user })
	}

	const handleFilterChange = (rights: PropertyRight[]) => {
		setSelectedRights(rights)
	}

	const handleDraftAccessRightsChange = useCallback((accessRights: AccessRightDTO[]) => {
		setDraftAccessRights(currentDraft => upsertAccessRightsDraft(currentDraft, accessRights))
	}, [])

	const determineUIState = useCallback((): ProjectModuleState => {
		const loadingAny = useCaseLoading || isLoading
		const errorAny = useCaseError || isError
		const dataAny = useCaseData != null && data != null

		if (errorAny) return ProjectModuleState.ERROR
		if (loadingAny) return ProjectModuleState.LOADING
		if (
			!dataAny ||
			!hasData(useCaseData) ||
			!data ||
			!data.classifications ||
			data.classifications.length <= 0
		)
			return ProjectModuleState.NO_DATA
		if (!selectedUserGroup || !selectedUseCase) return ProjectModuleState.SELECT_USER_GROUP_USE_CASE
		if (!data.classifications || data.classifications.length === 0)
			return ProjectModuleState.NO_DATA
		return ProjectModuleState.NORMAL
	}, [
		data,
		hasData,
		isError,
		isLoading,
		selectedUseCase,
		selectedUserGroup,
		useCaseData,
		useCaseError,
		useCaseLoading,
	])

	const handleQueryStateChange = useCallback(
		<T,>(
			loading: boolean,
			error: boolean,
			queryData: T[] | undefined,
			setLoading: (loading: boolean) => void,
			setError: (error: boolean) => void,
			setData: (data: T[] | undefined) => void,
		) => {
			setLoading(loading)
			setError(error)
			setData(queryData)
		},
		[],
	)

	useEffect(() => {
		setUiState(determineUIState())
	}, [determineUIState])

	const dataChecks: DataCheck[] = useMemo(
		() => [
			{
				dataType: 'a data model with object types',
				isAvailable: !!(data && data.classifications && data.classifications.length > 0),
			},
			{
				dataType: 'at least one UseCase',
				isAvailable: hasData(useCaseData),
			},
		],
		[data, hasData, useCaseData],
	)

	const handleCommitChanges = async () => {
		if (!currentUseCaseId || !currentUserGroupId) {
			toast.error('Please select a UseCase and a user group')
			return
		}

		if (!hasUncommittedChanges) {
			toast.info('No changes to save')
			return
		}

		try {
			const committedAccessRights = sortAccessRights(
				await commitAccessRightsMutation(draftAccessRights),
			)
			setOriginalAccessRights(committedAccessRights)
			setDraftAccessRights(committedAccessRights)
			queryClient.setQueryData(
				['accessRight', currentUserGroupId, currentUseCaseId],
				committedAccessRights,
			)
			toast.success(`Saved ${changedAccessRightsCount} changes`)
		} catch {
			toast.error('Failed to save access rights')
		}
	}

	const handleDiscardAndContinue = () => {
		setDraftAccessRights(originalAccessRights)
		setIsUnsavedDialogOpen(false)

		if (pendingSelection) {
			applyPendingSelection(pendingSelection)
		}

		setPendingSelection(null)
	}

	const handleStayOnCurrentContext = () => {
		setPendingSelection(null)
		setIsUnsavedDialogOpen(false)
	}

	return (
		<div className='grid h-full grid-cols-[1fr_3fr] overflow-hidden'>
			<div className='bg-project-module-background flex flex-col gap-5 p-6'>
				<SelectBar
					label='Guideline'
					idKey='id'
					nameKey='name'
					idBase='Guideline'
					hook={useGuidelines}
					onSelectedItemChange={setSelectedGuideline}
					onQueryStateChange={() => undefined}
					selectedItemId={selectedGuideline?.id ?? undefined}
					allOptionLabel='All Guidelines'
					onAllSelected={() => setSelectedGuideline(undefined)}
				/>
				<SelectBar
					label='UseCase'
					idKey='id'
					nameKey='name'
					idBase='UseCase'
					hook={useUseCases}
					onSelectedItemChange={handleUseCaseChange}
					selectedItemId={selectedUseCase?.id ?? ''}
					onQueryStateChange={(loading, error, queryData) => {
						handleQueryStateChange(
							loading,
							error,
							queryData,
							setUseCaseLoading,
							setUseCaseError,
							setUseCaseData,
						)
					}}
				/>
				<SelectBar
					label='User Group'
					idKey='id'
					nameKey='name'
					idBase='User Group'
					hook={useUserGroups}
					onSelectedItemChange={handleUserGroupChange}
					selectedItemId={selectedUserGroup?.id ?? ''}
					onQueryStateChange={() => undefined}
				/>
				<RightFilter label='Filter: ' onFilterChange={handleFilterChange} />
				<Button
					variant='contained'
					disabled={
						!selectedUseCase ||
						!selectedUserGroup ||
						!hasUncommittedChanges ||
						isCommittingAccessRights
					}
					onClick={handleCommitChanges}
				>
					{isCommittingAccessRights ? 'Saving…' : 'Save changes'}
				</Button>
				{hasUncommittedChanges ? (
					<p className='text-sm text-amber-700'>
						{changedAccessRightsCount} unsaved change{changedAccessRightsCount === 1 ? '' : 's'}
					</p>
				) : null}
			</div>
			<div className='relative'>
				<Tour
					tourKey={TOUR_KEY}
					moduleName={TOUR_MODULE_NAME}
					panels={TOUR_PANELS}
				/>
				{uiState === ProjectModuleState.ERROR ? <ErrorPage /> : null}
				{uiState === ProjectModuleState.LOADING ? <LoadingPage /> : null}
				{uiState === ProjectModuleState.SELECT_USER_GROUP_USE_CASE ? (
					<NoUserGroupOrUseCasePage />
				) : null}
				{uiState === ProjectModuleState.NO_DATA ? <NoDataPage dataChecks={dataChecks} /> : null}
				{uiState === ProjectModuleState.NORMAL ? (
					<MainPage
						userGroupId={selectedUserGroup?.id ?? ''}
						useCaseId={selectedUseCase?.id ?? ''}
						classificationList={data?.classifications || []}
						selectedFilter={selectedRights}
						accessRights={draftAccessRights}
						onAccessRightsChange={handleDraftAccessRightsChange}
					/>
				) : null}
			</div>
			<Dialog open={isUnsavedDialogOpen} onClose={handleStayOnCurrentContext}>
				<DialogTitle>Discard unsaved changes?</DialogTitle>
				<DialogContent>
					You have unsaved access-right changes for the current UseCase and user group. If you
					continue, those staged changes will be reverted.
				</DialogContent>
				<DialogActions>
					<Button onClick={handleStayOnCurrentContext}>Stay</Button>
					<Button color='warning' variant='contained' onClick={handleDiscardAndContinue}>
						Discard changes
					</Button>
				</DialogActions>
			</Dialog>
		</div>
	)
}

export default ProjectModule
