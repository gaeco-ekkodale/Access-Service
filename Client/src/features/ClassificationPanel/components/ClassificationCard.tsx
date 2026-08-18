// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Chip } from '@mui/material'
import Card from '@mui/material/Card'
import CardActionArea from '@mui/material/CardActionArea'
import CardContent from '@mui/material/CardContent'
import Typography from '@mui/material/Typography'
import { memo } from 'react'
import { ClassificationList } from '../../../api/AccessService'
import RightsChip from '../../../components/RightsChip'
import { ClassificationRight } from '../../../models/ClassificationRight'

type ClassificationProps = {
	cardClassification: ClassificationList
	classificationRight: ClassificationRight
	onClick?: (classification: ClassificationList) => void
}

const ClassificationCard = ({
	cardClassification,
	classificationRight,
	onClick,
}: ClassificationProps) => {
	return (
		<Card className='max-w-full' sx={{ height: '100%' }} onClick={() => onClick?.(cardClassification)}>
			<CardActionArea sx={{ height: '100%', alignItems: 'flex-start' }}>
				<CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 0.75, pb: '12px !important', width: '100%' }}>
					{cardClassification.guidelineName ? (
						<Chip
							label={cardClassification.guidelineName}
							size='small'
							variant='outlined'
							color='primary'
							sx={{ alignSelf: 'flex-start', fontSize: '0.68rem', height: 20 }}
						/>
					) : null}
					<Typography variant='subtitle1' component='div' fontWeight={600} sx={{ lineHeight: 1.3 }}>
						{cardClassification.name}
					</Typography>
					{cardClassification.code ? (
						<Typography variant='caption' color='text.secondary' sx={{ lineHeight: 1 }}>
							{cardClassification.code}
						</Typography>
					) : null}
					<div className='flex items-center gap-2 flex-wrap'>
						<Chip
							size='small'
							className='bg-green-100'
							label={cardClassification.propertyCount?.toString()}
						/>
						<RightsChip classificationRight={classificationRight} />
					</div>
				</CardContent>
			</CardActionArea>
		</Card>
	)
}

export default memo(ClassificationCard)
