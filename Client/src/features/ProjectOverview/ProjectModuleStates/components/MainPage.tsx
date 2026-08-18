// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { AccessRightDTO } from '../../../../api/AccessService';
import ClassificationPanel, { ClassificationPanelProps } from '../../../ClassificationPanel/components/ClassificationPanel';

type MainPageProps = ClassificationPanelProps & {
	accessRights: AccessRightDTO[]
	onAccessRightsChange: (accessRights: AccessRightDTO[]) => void
}

const MainPage: React.FC<MainPageProps> = ({
	userGroupId,
	useCaseId,
	classificationList,
	selectedFilter,
	accessRights,
	onAccessRightsChange,
}) => (
	<ClassificationPanel
		userGroupId={userGroupId}
		useCaseId={useCaseId}
		classificationList={classificationList}
		selectedFilter={selectedFilter}
		accessRights={accessRights}
		onAccessRightsChange={onAccessRightsChange}
	/>
)

export default MainPage
