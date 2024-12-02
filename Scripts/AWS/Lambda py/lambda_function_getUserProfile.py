import json
import boto3
import logging
from decimal import Decimal
def lambda_handler(event, context):
    print("Received event:", json.dumps(event, indent=2))
    print("Context:", context)
logger = logging.getLogger()
logger.setLevel(logging.INFO)

dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('MOFITUserProfiles')

class DecimalEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, Decimal):
            return float(obj)
        return super(DecimalEncoder, self).default(obj)

def lambda_handler(event, context):
    logger.info(f"Received event: {json.dumps(event)}")
    try:
        # Get userId from path parameters
        user_id = event['pathParameters']['userID']
        logger.info(f"Extracted userId: {user_id}")
        
        # Get the item from DynamoDB
        response = table.get_item(Key={'UserId': user_id})
        
        # Check if the item exists
        if 'Item' in response:
            # Convert DynamoDB item to a JSON-serializable format
            item = json.loads(json.dumps(response['Item'], cls=DecimalEncoder))
            return {
                'statusCode': 200,
                'body': json.dumps(item)
            }
        else:
            return {
                'statusCode': 404,
                'body': json.dumps({'message': 'User not found'})
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