import json
import boto3
import logging
from decimal import Decimal
from boto3.dynamodb.conditions import Key

logger = logging.getLogger()
logger.setLevel(logging.INFO)

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('MOFITWorkouts')

class DecimalEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, Decimal):
            return float(obj)
        return super(DecimalEncoder, self).default(obj)

def lambda_handler(event, context):
    logger.info(f"Received event: {json.dumps(event)}")
    try:
        # Get userId from path parameters
        user_id = event['pathParameters']['userId']
        logger.info(f"Getting workouts for userId: {user_id}")
        
        # Query DynamoDB for all workouts for this user
        response = table.query(
            KeyConditionExpression=Key('UserId').eq(user_id),
            ScanIndexForward=False  # Sort in descending order (newest first)
        )
        
        # Check if there are any workouts
        if 'Items' in response:
            # Convert DynamoDB items to JSON-serializable format
            items = json.loads(json.dumps(response['Items'], cls=DecimalEncoder))
            return {
                'statusCode': 200,
                'headers': {
                    'Access-Control-Allow-Origin': '*',
                    'Access-Control-Allow-Headers': 'Content-Type',
                    'Access-Control-Allow-Methods': 'GET'
                },
                'body': json.dumps({
                    'workouts': items,
                    'count': len(items)
                })
            }
        else:
            return {
                'statusCode': 404,
                'body': json.dumps({'message': 'No workouts found for user'})
            }
    except KeyError as e:
        return {
            'statusCode': 400,
            'body': json.dumps({'message': f'Missing required parameter: {str(e)}'})
        }
    except Exception as e:
        logger.error(f"Error: {str(e)}")
        return {
            'statusCode': 500,
            'body': json.dumps({'message': str(e)})
        }